using NBitcoin.Secp256k1;
using NostrSharp.Extensions;
using NostrSharp.Nostr.Enums;
using System;
using System.Security.Cryptography;

namespace NostrSharp.Keys
{
    public class NSec : IEquatable<NSec>
    {
        public string Hex { get; }
        public string Bech32 { get; }
        public ECPrivKey Ec { get; }


        private NSec(string hex, string bech32, ECPrivKey ec)
        {
            Hex = hex;
            Bech32 = bech32;
            Ec = ec;
        }


        /// <summary>
        /// Genera una nuova chiave privata
        /// </summary>
        public static NSec New()
        {
            using RandomNumberGenerator randomGen = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[32];
            randomGen.GetBytes(randomBytes);
            string randomHex = randomBytes.ToHexString();
            return FromHex(randomHex);
        }


        /// <summary>
        /// Derive public key from this private key
        /// </summary>
        public NPub DerivePublicKey()
        {
            return NPub.FromPrivateEc(Ec);
        }
        /// <summary>
        /// Derive shared key between secret and public one
        /// </summary>
        public NPub DeriveSharedKey(NPub publicKey)
        {
            // 32 + 1 byte for the compression (0x02) prefix.
            Span<byte> input = stackalloc byte[33];
            input[0] = 0x02;
            publicKey.Ec.WriteToSpan(input[1..]);

            bool success = Context.Instance.TryCreatePubKey(input, out ECPubKey? prefixedPublicKey);
            if (!success || prefixedPublicKey == null)
                throw new InvalidOperationException("Can't create prefixed public key");

            ECPubKey sharedKey = prefixedPublicKey.GetSharedPubkey(Ec);
            if (sharedKey == null)
                throw new InvalidOperationException("Can't create shared public key");

            return NPub.FromEc(sharedKey.ToXOnlyPubKey());
        }


        /// <summary>
        /// Sign hex with the private key. 
        /// Returns signature in hex format.
        /// </summary>
        public string? SignHex(string? hex)
        {
            if (hex == null)
                return null;

            byte[] hexBytes = hex.HexToByteArray();
            return !Ec.TrySignBIP340(hexBytes, null, out SecpSchnorrSignature? signature) ? null : signature.ToBytes().ToHexString();
        }


        public static NSec FromHex(string hex)
        {
            ECPrivKey ec = ECPrivKey.Create(hex.HexToByteArray());
            string bech32 = hex.HexToNsecBech32() ?? string.Empty;
            return new NSec(hex, bech32, ec);
        }
        public static NSec FromBech32(string bech32)
        {
            string? hex = bech32.Bech32ToHexKey(out string? hrp);
            if (!Bech32Identifiers.NSec.Equals(hrp, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Provided bech32 key is not 'nsec'", nameof(bech32));
            return FromHex(hex);
        }
        public static NSec FromEc(ECPrivKey ec)
        {
            string hex = ToHex(ec);
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Provided ec key is not correct", nameof(ec));
            return FromHex(hex);
        }


        public static string ToHex(ECPrivKey key)
        {
            Span<byte> keySpan = stackalloc byte[65];
            key.WriteToSpan(keySpan);
            string keyHex = keySpan.ToHexString();
            return keyHex[..64];
        }


        public bool Equals(NSec? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Hex, other.Hex, StringComparison.OrdinalIgnoreCase) && string.Equals(Bech32, other.Bech32, StringComparison.OrdinalIgnoreCase);
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((NSec)obj);
        }


        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(Hex, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Bech32, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }
    }
}
