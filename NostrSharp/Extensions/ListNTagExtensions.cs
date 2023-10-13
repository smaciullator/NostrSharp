using BTCPayServer.Lightning;
using Newtonsoft.Json;
using NostrSharp.Json;
using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NostrSharp.Extensions
{
    public static class NTagUtilities
    {
        public static void AddETag(this List<NTag> tags, string value, string? secondValue = null, string? thirdValue = null, string? fourthValue = null)
        {
            tags.Add(new(TagKeys.EventIdentifier, value, secondValue, thirdValue, fourthValue));
        }
        public static void AddPTag(this List<NTag> tags, string identifier, string? preferredRelay = null, string? role = null, string? proof = null)
        {
            tags.Add(new(TagKeys.ProfileIdentifier, identifier, preferredRelay, role, proof));
        }
        public static void AddDTag(this List<NTag> tags, string identifier)
        {
            tags.Add(new(TagKeys.ReplaceableIdentifier, identifier));
        }
        public static void AddATag(this List<NTag> tags, string coordinates, string? preferredRelay = null, string? marker = null)
        {
            tags.Add(new(TagKeys.CoordinatesIdentifier, coordinates, preferredRelay, marker));
        }
        public static void AddKTag(this List<NTag> tags, NKind kind)
        {
            tags.Add(new(TagKeys.AddKTag, ((int)kind).ToString()));
        }
        public static void AddExternalLinkTag(this List<NTag> tags, string externalLink, string? additionalData = null)
        {
            tags.Add(new(TagKeys.ExternalLinkIdentifier, externalLink, additionalData));
        }
        public static void AddXTag(this List<NTag> tags, string sha256HexEncodedFileString)
        {
            tags.Add(new(TagKeys.XIdentifier, sha256HexEncodedFileString));
        }
        public static void AddUTag(this List<NTag> tags, string absoluteUrl)
        {
            tags.Add(new(TagKeys.UIdentifier, absoluteUrl));
        }


        public static void AddITag(this List<NTag> tags, string parameter, string? proof = null)
        {
            tags.Add(new(TagKeys.ExternalIdentityIdentifier, parameter, proof));
        }
        public static void AddTTag(this List<NTag> tags, string hashtag)
        {
            tags.Add(new(TagKeys.HashtagIdentifier, hashtag));
        }

        public static void AddMTag(this List<NTag> tags, string mimeType)
        {
            tags.Add(new(TagKeys.MimeTypeIdentifier, mimeType.ToLowerInvariant()));
        }

        public static void AddGTag(this List<NTag> tags, string geohash)
        {
            tags.Add(new(TagKeys.GeohashIdentifier, geohash));
        }
        public static void AddLocationTag(this List<NTag> tags, string location)
        {
            tags.Add(new(TagKeys.LocationIdentifier, location));
        }


        public static void AddAmountTag(this List<NTag> tags, decimal satoshisAmount)
        {
            tags.Add(new(TagKeys.AmountIdentifier, LightMoney.FromUnit(satoshisAmount, LightMoneyUnit.Satoshi).MilliSatoshi.ToString()));
        }
        public static void AddLNURLTag(this List<NTag> tags, string bech32LNURL)
        {
            tags.Add(new(TagKeys.LNURLIdentifier, bech32LNURL));
        }


        public static void AddRelayTag(this List<NTag> tags, string relayUri, string? customMarker = null)
        {
            tags.Add(new(TagKeys.RelayIdentifier, relayUri, customMarker));
        }
        public static void AddPreferredRelayTag(this List<NTag> tags, List<string> preferredRelaysUris)
        {
            tags.Add(new(TagKeys.PreferredRelaysIdentifier, preferredRelaysUris.Distinct().ToArray()));
        }
        public static void AddChallengeTag(this List<NTag> tags, string challenge)
        {
            tags.Add(new(TagKeys.ChallengeIdentifier, challenge));
        }
        public static void AddSubjectTag(this List<NTag> tags, string challenge)
        {
            tags.Add(new(TagKeys.SubjectIdentifier, challenge));
        }


        public static void AddTitleTag(this List<NTag> tags, string title)
        {
            tags.Add(new(TagKeys.TitleIdentifier, title));
        }
        public static void AddImageTag(this List<NTag> tags, string imageUrl, string? imageSizeInPixels = null)
        {
            tags.Add(new(TagKeys.ImageIdentifier, imageUrl, imageSizeInPixels));
        }
        public static void AddThumbTag(this List<NTag> tags, string thumbUrl, string? thumbSizeInPixels = null)
        {
            tags.Add(new(TagKeys.ThumbIdentifier, thumbUrl, thumbSizeInPixels));
        }
        public static void AddSummaryTag(this List<NTag> tags, string summary)
        {
            tags.Add(new(TagKeys.SummaryIdentifier, summary));
        }


        public static void AddGoalTag(this List<NTag> tags, string goalEventId, string? preferredRelayUrl = null)
        {
            tags.Add(new(TagKeys.GoalIdentifier, goalEventId, preferredRelayUrl));
        }
        public static void AddZapTag(this List<NTag> tags, string hexPubKey, string preferredRelayUrl, decimal? weight = null)
        {
            tags.Add(new(TagKeys.ZapIdentifier, hexPubKey, preferredRelayUrl, weight.ToString()));
        }


        public static void AddNameTag(this List<NTag> tags, string name)
        {
            tags.Add(new(TagKeys.NameIdentifier, name));
        }
        public static void AddDescriptionTag(this List<NTag> tags, string description)
        {
            tags.Add(new(TagKeys.DescriptionIdentifier, description));
        }


        public static void AddUrlTag(this List<NTag> tags, string url)
        {
            tags.Add(new(TagKeys.UrlIdentifier, url));
        }


        public static void AddStatusTag(this List<NTag> tags, NLiveEventStatus status)
        {
            tags.Add(new(TagKeys.StatusIdentifier, status.ToString()));
        }
        public static void AddStreamingTag(this List<NTag> tags, string streamingUrl)
        {
            tags.Add(new(TagKeys.StreamingIdentifier, streamingUrl));
        }
        public static void AddRecordingTag(this List<NTag> tags, string recordingUrl)
        {
            tags.Add(new(TagKeys.RecordingIdentifier, recordingUrl));
        }
        public static void AddStartAtTag(this List<NTag> tags, DateTime startAt)
        {
            tags.Add(new(TagKeys.StartsAtIdentifier, startAt.ToUnixTimeStamp().ToString()));
        }
        public static void AddEndAtTag(this List<NTag> tags, DateTime endAt)
        {
            tags.Add(new(TagKeys.EndsAtIdentifier, endAt.ToUnixTimeStamp().ToString()));
        }
        public static void AddClosedAtTag(this List<NTag> tags, DateTime closeAt)
        {
            tags.Add(new(TagKeys.ClosedAtIdentifier, closeAt.ToUnixTimeStamp().ToString()));
        }
        public static void AddCurrentParticipantsTag(this List<NTag> tags, int currentParticipantsNumber)
        {
            tags.Add(new(TagKeys.CurrentParticipantsIdentifier, currentParticipantsNumber.ToString()));
        }
        public static void AddTotalParticipantsTag(this List<NTag> tags, int totalParticipantsNumber)
        {
            tags.Add(new(TagKeys.TotalParticipantsIdentifier, totalParticipantsNumber.ToString()));
        }


        public static void AddExpirationTag(this List<NTag> tags, DateTime expiration)
        {
            tags.Add(new(TagKeys.ExpirationIdentifier, expiration.ToUnixTimeStamp().ToString()));
        }
        public static void AddPublishedAtTag(this List<NTag> tags, DateTime publishedAtUTC)
        {
            tags.Add(new(TagKeys.PublishedAtIdentifier, publishedAtUTC.ToUnixTimeStamp().ToString()));
        }
        public static void AddStartDateTag(this List<NTag> tags, DateTime startDate)
        {
            tags.Add(new(TagKeys.StartIdentifier, startDate.ToString("YYYY-MM-DD")));
        }
        public static void AddStartTimeTag(this List<NTag> tags, DateTime startDate)
        {
            tags.Add(new(TagKeys.StartIdentifier, startDate.ToUnixTimeStamp().ToString()));
        }
        public static void AddStartTimezoneTag(this List<NTag> tags, string timezone)
        {
            tags.Add(new(TagKeys.StartTimezoneIdentifier, timezone));
        }
        public static void AddEndDateTag(this List<NTag> tags, DateTime endDate)
        {
            tags.Add(new(TagKeys.EndIdentifier, endDate.ToString("YYYY-MM-DD")));
        }
        public static void AddEndTimeTag(this List<NTag> tags, DateTime endDate)
        {
            tags.Add(new(TagKeys.EndIdentifier, endDate.ToUnixTimeStamp().ToString()));
        }
        public static void AddEndTimezoneTag(this List<NTag> tags, string timezone)
        {
            tags.Add(new(TagKeys.EndTimezoneIdentifier, timezone));
        }


        public static void AddProxyTag(this List<NTag> tags, string identifier, string? type)
        {
            tags.Add(new(TagKeys.ProxyIdentifier, identifier, type));
        }


        public static void AddEmojiTag(this List<NTag> tags, string unemojifiedName, string? url = null)
        {
            tags.Add(new(TagKeys.EmojiIdentifier, unemojifiedName, url));
        }
        public static void AddLabelTag(this List<NTag> tags, Label label)
        {
            tags.Add(new(TagKeys.LabelNamespaceIdentifier, label.Namespace));
            string? metadata = null;
            if (label.Metadata is not null)
                metadata = JsonConvert.SerializeObject(label.Metadata, SerializerCustomSettings.Settings);
            tags.Add(new(TagKeys.LabelIdentifier, label.Name, label.Namespace, metadata));
            tags.AddRange(label.References);
        }


        public static void AddContentWarningTag(this List<NTag> tags, string reason)
        {
            tags.Add(new(TagKeys.ContentWarningIdentifier, reason));
        }


        public static void AddAes256GcmTag(this List<NTag> tags, string key, string nonce)
        {
            tags.Add(new(TagKeys.Aes256GcmIdentifier, key, nonce));
        }
        public static void AddSizeTag(this List<NTag> tags, string size)
        {
            tags.Add(new(TagKeys.SizeIdentifier, size));
        }
        public static void AddDimTag(this List<NTag> tags, string dim)
        {
            tags.Add(new(TagKeys.DimIdentifier, dim));
        }
        public static void AddMagnetTag(this List<NTag> tags, string magnetLink)
        {
            tags.Add(new(TagKeys.MagnetIdentifier, magnetLink));
        }
        public static void AddBlurhashTag(this List<NTag> tags, string blurhash)
        {
            tags.Add(new(TagKeys.BlurhashIdentifier, blurhash));
        }


        public static void AddMethodTag(this List<NTag> tags, string method)
        {
            tags.Add(new(TagKeys.MethodIdentifier, method));
        }
        public static void AddPayloadTag(this List<NTag> tags, string payload)
        {
            tags.Add(new(TagKeys.PayloadIdentifier, payload));
        }


        public static void AddPriceTag(this List<NTag> tags, int price, string currencyCodeISO4217, ClassifiedListingRecurringPrice? recurringPrice)
        {
            tags.Add(new(TagKeys.PriceIdentifier, price.ToString(), currencyCodeISO4217, recurringPrice.ToString()));
        }
    }
}
