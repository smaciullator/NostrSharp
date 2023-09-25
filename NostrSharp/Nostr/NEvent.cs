using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NostrSharp.Extensions;
using NostrSharp.Keys;
using NostrSharp.Nostr.Enums;
using NostrSharp.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NostrSharp.Nostr
{
    [JsonConverter(typeof(NParser))]
    public class NEvent : IEquatable<NEvent>, IDisposable
    {
        /// <summary>
        /// To obtain the event.id, we sha256 the serialized event.
        /// The serialization is done over the UTF-8 JSON-serialized string (with no white space or line breaks) of the following structure:
        /// [
        ///   0,
        ///   <pubkey, as a lowercase hex string>,
        ///   <created_at, as a number>,
        ///   <kind, as a number>,
        ///   <tags, as an array of arrays of non-null strings>,
        ///   <content, as a string>
        /// ]
        /// </summary>
        [JsonProperty(propertyName: "id")]
        public string? Id { get; private set; }

        /// <summary>
        /// 32-bytes lowercase hex-encoded public key of the event creator
        /// </summary>
        [JsonProperty(propertyName: "pubkey")]
        public string? PubKey { get; private set; }

        [JsonProperty(propertyName: "created_at")]
        public DateTime? CreatedAt { get; private set; } = DateTime.UtcNow;

        [JsonProperty(propertyName: "kind")]
        public int NumericKind { get; private set; }
        [JsonIgnore]
        public NKind Kind
        {
            get
            {
                if (!Enum.TryParse<NKind>(NumericKind.ToString(), out NKind kind))
                    return NKind.CustomKindNotParsable;
                return kind;
            }
            private set
            {
                NumericKind = (int)value;
            }
        }

        [JsonProperty(propertyName: "tags")]
        public List<NTag> Tags { get; private set; }

        /// <summary>
        /// Arbitrary string
        /// </summary>
        [JsonProperty(propertyName: "content")]
        public string? Content { get; private set; }

        /// <summary>
        /// 64-bytes hex of the signature of the sha256 hash of the serialized event data, which is the same as the "id" field
        /// </summary>
        [JsonProperty(propertyName: "sig")]
        public string? Sig { get; private set; }


        [JsonIgnore]
        public bool Signed => !string.IsNullOrEmpty(Sig);


        public NEvent() { }
        public NEvent(NKind kind, List<NTag> tags) : this(kind, tags, null) { }
        public NEvent(NKind kind, string? content) : this(kind, new(), content) { }
        public NEvent(NKind kind, List<NTag> tags, string? content) : this((int)kind, tags, content) { }
        public NEvent(int kind, List<NTag> tags, string? content)
        {
            NumericKind = kind;
            Tags = new List<NTag>(tags.DistinctBy(x => new { x.Key, x.Value }).Select(x => new NTag(x.Key, x.Value, x.SecondValue, x.ThirdValue, x.FourthValue)).ToList());
            Content = content;
        }
        /// <summary>
        /// Usato per creare un'istanza della quale calcolare l'Id
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="createdAt"></param>
        /// <param name="kind"></param>
        /// <param name="tags"></param>
        /// <param name="content"></param>
        private NEvent(string? id, string? pubkey, DateTime? createdAt, NKind kind, List<NTag> tags, string? content)
        {
            Id = id;
            PubKey = pubkey;
            CreatedAt = createdAt;
            Kind = kind;
            Tags = new List<NTag>(tags.DistinctBy(x => new { x.Key, x.Value }).Select(x => new NTag(x.Key, x.Value, x.SecondValue, x.ThirdValue, x.FourthValue)).ToList());
            Content = content;
        }


        public List<NTag> GetTagsByKey(string key)
        {
            return (Tags ?? new List<NTag>()).Where(x => x.Key == key).ToList();
        }
        public string? GetFirstProfileTag()
        {
            List<NTag> profileTags = GetTagsByKey(TagKeys.ProfileIdentifier);
            if (profileTags.Count == 0)
            {
                //throw new InvalidOperationException("Recipient pubkey is not specified, can't encrypt");
                return null;
            }
            NTag firstRecipient = profileTags.First();
            return firstRecipient.Value;
        }


        public string GetCoordinates()
        {
            List<NTag> replaceableTags = GetTagsByKey(TagKeys.ReplaceableIdentifier);
            if (replaceableTags.Count == 0)
                return $"{NumericKind}:{PubKey}:";
            return $"{NumericKind}:{PubKey}:{replaceableTags.First().Value}";
        }


        #region Signing
        /// <summary>
        /// Firma l'evento usando la NSec fornita
        /// </summary>
        public bool Sign(NSec privateKey)
        {
            if (Signed)
                return true;

            PubKey = privateKey.DerivePublicKey().Hex;
            CreatedAt = DateTime.UtcNow;
            Id = GetId();
            if (!string.IsNullOrEmpty(Id))
                Sig = privateKey.SignHex(Id);
            return Sig is not null;
        }
        public bool IsVerified()
        {
            if (string.IsNullOrEmpty(PubKey) || string.IsNullOrEmpty(Sig))
                return false;

            try
            {
                return NPub.FromHex(PubKey).IsHexSignatureValid(Sig, GetId());
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        /// <summary>
        /// Se l'Id non è ancora stato calcolato:
        ///     - crea una copia dell'evento usando un placeholder come Id.
        ///     - la copia viene serializzata in json, e il placeholder di tipo stringa sulla proprietà Id viene
        ///       sostituito con il valore numerico 0.
        ///     - viene poi elaborato lo sha256 del risultato della sostituzione, preso l'hex e salvato in Id
        /// </summary>
        private string? GetId()
        {
            string? id = null;
            try
            {
                using (NEventSign temp = new NEventSign(PubKey, CreatedAt.Value, Kind, Tags, Content))
                    id = JsonConvert.SerializeObject(temp, SerializerCustomSettings.EventSignSettings).GetSha256().ToHexString();
            }
            catch (Exception ex)
            {
                id = null;
            }
            return id;
        }
        #endregion


        #region Encryption
        [JsonIgnore]
        private const string IvSeparator = "?iv=";


        /// <summary>
        /// It derive a shared key between the first "p" tag value of this event and the given private key,
        /// and then encrypt the Content
        /// </summary>
        public bool Encrypt(NSec senderKey)
        {
            try
            {
                string? firstRecipient = GetFirstProfileTag();
                if (string.IsNullOrEmpty(firstRecipient))
                    return false;
                NPub recipientPubkey = NPub.FromHex(firstRecipient);
                NPub sharedKey = senderKey.DeriveSharedKey(recipientPubkey);

                if (!EncryptBase64((Content ?? "").UTF8AsByteArray(), sharedKey, out string? encryptedText, out string? iv))
                    return false;
                if (string.IsNullOrEmpty(encryptedText) || string.IsNullOrEmpty(iv))
                    return false;

                PubKey = senderKey.DerivePublicKey().Hex;
                CreatedAt = DateTime.UtcNow;
                Kind = Kind;
                Content = $"{encryptedText}{IvSeparator}{iv}";
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// For those platforms that do not support AES-ECB encryption you can override the encryption part
        /// by passing in a Func that has:
        ///     - first input parameter is shared key's EC as byte array
        ///     - second input parameter is UTF8 plain text as byte array
        ///     - output parameter is in this format: encrypted_text + "?iv=" + aes_iv
        /// </summary>
        /// <param name="senderKey"></param>
        /// <param name="overrideEncryptionMethod"></param>
        /// <returns></returns>
        public async Task<bool> Encrypt(NSec senderKey, Func<byte[], byte[], Task<string?>> overrideEncryptionMethod)
        {
            try
            {
                string? firstRecipient = GetFirstProfileTag();
                if (string.IsNullOrEmpty(firstRecipient))
                    return false;
                NPub recipientPubkey = NPub.FromHex(firstRecipient);
                NPub sharedKey = senderKey.DeriveSharedKey(recipientPubkey);

                string encryptedTextPlusIV = await overrideEncryptionMethod(sharedKey.Ec.ToBytes(), (Content ?? "").UTF8AsByteArray());
                if (string.IsNullOrEmpty(encryptedTextPlusIV) || !encryptedTextPlusIV.Contains(IvSeparator))
                    return false;

                PubKey = senderKey.DerivePublicKey().Hex;
                CreatedAt = DateTime.UtcNow;
                Kind = Kind;
                Content = encryptedTextPlusIV;
                return true;
            }
            catch (Exception ex) { return false; }
        }
        /// <summary>
        /// Decrypt content text using the given private key
        /// </summary>
        public string? Decrypt(NSec privateKey)
        {
            if (PubKey is null)
                throw new InvalidOperationException("Sender pubkey is not specified, can't decrypt");
            string? recipientPubkey = GetFirstProfileTag();
            if (string.IsNullOrEmpty(recipientPubkey) || string.IsNullOrWhiteSpace(recipientPubkey))
                throw new InvalidOperationException("Recipient pubkey is not specified, can't decrypt");
            GetEncryptedData(out string? encryptedContent, out string? iv);
            if (string.IsNullOrEmpty(encryptedContent) || string.IsNullOrWhiteSpace(encryptedContent))
                throw new InvalidOperationException("Encrypted content is null, can't decrypt");
            if (string.IsNullOrEmpty(iv) || string.IsNullOrWhiteSpace(iv))
                throw new InvalidOperationException("Initialization vector is null, can't decrypt");

            string hexNPub = privateKey.DerivePublicKey().Hex;
            string targetPubkeyHex = "";
            if (PubKey == hexNPub)
                targetPubkeyHex = recipientPubkey;
            else if (recipientPubkey == hexNPub)
                targetPubkeyHex = PubKey;
            else
                throw new InvalidOperationException("The encrypted event is not for the given private key. Sender or receiver pubkey doesn't match");

            return DecryptBase64(encryptedContent, iv, privateKey.DeriveSharedKey(NPub.FromHex(targetPubkeyHex)));
        }
        /// <summary>
        /// For those platforms that do not support AES-ECB decryption you can override the decryption part
        /// by passing in a Func that has:
        ///     - first input parameter is shared key's EC as byte array
        ///     - second input parameter is IV as byte array
        ///     - third input parameter is encrypted text as byte array
        ///     - output parameter is in the plain text decrypted string
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="overrideDecryptionMethod"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string?> Decrypt(NSec privateKey, Func<byte[], byte[], byte[], Task<string?>> overrideDecryptionMethod)
        {
            if (PubKey is null)
                throw new InvalidOperationException("Sender pubkey is not specified, can't decrypt");
            string? recipientPubkey = GetFirstProfileTag();
            if (string.IsNullOrEmpty(recipientPubkey) || string.IsNullOrWhiteSpace(recipientPubkey))
                throw new InvalidOperationException("Recipient pubkey is not specified, can't decrypt");
            GetEncryptedData(out string? encryptedContent, out string? iv);
            if (string.IsNullOrEmpty(encryptedContent) || string.IsNullOrWhiteSpace(encryptedContent))
                throw new InvalidOperationException("Encrypted content is null, can't decrypt");
            if (string.IsNullOrEmpty(iv) || string.IsNullOrWhiteSpace(iv))
                throw new InvalidOperationException("Initialization vector is null, can't decrypt");

            string hexNPub = privateKey.DerivePublicKey().Hex;
            string targetPubkeyHex = "";
            if (PubKey == hexNPub)
                targetPubkeyHex = recipientPubkey;
            else if (recipientPubkey == hexNPub)
                targetPubkeyHex = PubKey;
            else
                throw new InvalidOperationException("The encrypted event is not for the given private key. Sender or receiver pubkey doesn't match");

            return await overrideDecryptionMethod(privateKey.DeriveSharedKey(NPub.FromHex(targetPubkeyHex)).Ec.ToBytes(), Convert.FromBase64String(iv), Convert.FromBase64String(encryptedContent));
        }
        private void GetEncryptedData(out string? encriptedContent, out string? iv)
        {
            encriptedContent = null;
            iv = null;

            if (string.IsNullOrEmpty(Content) || string.IsNullOrWhiteSpace(Content) || !Content.Contains(IvSeparator))
                return;

            string[] split = Content.Split(IvSeparator);
            encriptedContent = split.Length >= 1 ? split[0] : null;
            iv = split.Length >= 2 ? split[1] : null;
            split = null;
        }


        private bool EncryptBase64(byte[] plainText, NPub key, out string? encryptedText, out string? iv)
        {
            encryptedText = null;
            iv = null;
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = key.Ec.ToBytes();
                // NOTE: this is not supported on Blazor WebAssembly and will throw a PlatformNotSupportedException
                // reference: https://github.com/dotnet/designs/blob/main/accepted/2021/blazor-wasm-crypto.md#blazor-wasm--js-interop-example
                byte[] cbcEncrypted = aes.EncryptCbc(plainText, aes.IV);
                encryptedText = Convert.ToBase64String(cbcEncrypted);
                iv = Convert.ToBase64String(aes.IV);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private string DecryptBase64(string encryptedContent, string iv, NPub key)
        {
            try
            {
                byte[] textBytes = Convert.FromBase64String(encryptedContent);
                byte[] ivBytes = Convert.FromBase64String(iv);

                using Aes aes = Aes.Create();
                aes.Key = key.Ec.ToBytes().ToArray();
                return aes.DecryptCbc(textBytes, ivBytes).ToUTF8String();
            }
            catch { return ""; }
        }
        #endregion


        #region Misc
        public bool Equals(NEvent? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id;
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((NEvent)obj);
        }


        public override int GetHashCode()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }
        #endregion


        public void Dispose()
        {
            if (Tags is not null)
            {
                foreach (NTag tag in Tags)
                    tag.Dispose();
                Tags.Clear();
            }
        }
    }
}
