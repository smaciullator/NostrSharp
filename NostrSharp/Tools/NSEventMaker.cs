using Newtonsoft.Json;
using NostrSharp.Extensions;
using NostrSharp.Keys;
using NostrSharp.Nostr;
using NostrSharp.Nostr.Enums;
using NostrSharp.Nostr.Models;
using NostrSharp.Nostr.Models.Channels;
using NostrSharp.Nostr.Models.Marketplace;
using NostrSharp.Nostr.Models.Tags;
using NostrSharp.Relay.Models;
using NostrSharp.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NostrSharp.Tools
{
    public static class NSEventMaker
    {
        public static NEvent SetMetadata(string? name, string? username, string? displayName, string? display_name, string? about, string? picture,
            string? banner, string? website, string? nip05, string? lud16, string? lud06, List<ExternalIdentity>? externalIdentities = null, List<Emoji>? customEmojis = null)
        {

            List<NTag> tags = new();
            // Se in name e/o about c'è almeno una emoji
            if (customEmojis is not null)
                foreach (Emoji? emoji in customEmojis.Where(x =>
                    x.IsCustom
                    && (
                        (name is not null && x.EmojifiedName is not null && name.Contains(x.EmojifiedName))
                        || (about is not null && x.EmojifiedName is not null && about.Contains(x.EmojifiedName))
                    )
                ))
                    tags.AddEmojiTag(emoji.UnemojifiedName, emoji.Url);

            if (externalIdentities is not null)
                foreach (ExternalIdentity identity in externalIdentities)
                    tags.AddITag(identity.Parameter, identity.Proof);

            return new NEvent(NKind.Metadata, tags, JsonConvert.SerializeObject(new UserMetadata(name, username, displayName, display_name, about, picture, banner, website, nip05, lud16, lud06), SerializerCustomSettings.Settings));
        }
        
        /// <summary>
        /// NIP02: Contact List
        /// reference: https://github.com/nostr-protocol/nips/blob/70ede5e67d3631b109dd16a811d236b4065eb44d/02.md
        /// </summary>
        /// <param name="contacts"></param>
        /// <param name="relaysList"></param>
        /// <returns></returns>
        public static NEvent ContactList(IList<Contact> contacts, IList<RelayInfo> relaysList)
        {
            List<NTag> tags = new();
            foreach (Contact contact in contacts)
                tags.AddPTag(contact.ContactPubKey, contact.PreferredRelayUri, contact.PetName);

            string relaysInfo = "";
            foreach (RelayInfo relay in relaysList)
                relaysInfo += JsonConvert.SerializeObject(relay, SerializerCustomSettings.Settings).Replace("[", "").Replace("]", "") + ",";

            string? content = string.IsNullOrEmpty(relaysInfo) ? null : "{" + relaysInfo.Remove(relaysInfo.Length - 1) + "}";

            return new NEvent(NKind.Contacts, tags, content);
        }
        
        /// <summary>
        /// NIP67: Relay List Metadata
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/65.md
        /// </summary>
        /// <param name="relaysList"></param>
        /// <returns></returns>
        public static NEvent RelayListMetadata(IList<RelayInfo> relaysList)
        {
            List<NTag> tags = new();
            foreach (RelayInfo relay in relaysList)
            {
                string? explicitPermissions = null;
                if (relay.RelayPermissions.Read && !relay.RelayPermissions.Write)
                    explicitPermissions = "read";
                else if (!relay.RelayPermissions.Read && relay.RelayPermissions.Write)
                    explicitPermissions = "write";
                tags.AddExternalLinkTag(relay.RelayUri.ToString(), explicitPermissions);
            }
            return new NEvent(NKind.Contacts, tags);
        }

        /// <summary>
        /// NIP42: Authentication of Clients to Relays
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/42.md
        /// </summary>
        /// <param name="relayUri"></param>
        /// <param name="challengeString"></param>
        /// <returns></returns>
        public static NEvent Authentication(Uri relayUri, string challengeString)
        {
            List<NTag> tags = new();
            tags.AddRelayTag(relayUri.ToString());
            tags.AddChallengeTag(challengeString);
            return new NEvent(NKind.ClientAuthentication, tags);
        }


        /// <summary>
        /// Generate a Kind 1 event
        /// </summary>
        /// <param name="content"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="subject"></param>
        /// <param name="customEmojis"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent ShortTextNote(string content, List<PTag> profileMentions = null, List<PTag> eventMentions = null, Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, string subject = "", List<Emoji>? customEmojis = null, DateTime? expiration = null,
            NProxy? proxy = null, ATag? communityDefinitionEvent = null)
        {
            List<NTag> tags = new();

            if (profileMentions is not null)
                foreach (PTag profile in profileMentions)
                    tags.AddPTag(profile.PubKeyHex, profile.PreferredRelay);

            if (eventMentions is not null)
                foreach (PTag ev in eventMentions)
                    tags.AddETag(ev.PubKeyHex, ev.PreferredRelay);

            if (!string.IsNullOrEmpty(subject))
                tags.AddSubjectTag(subject);

            if (contentWarning is not null)
                tags.AddContentWarningTag(contentWarning.Reason);

            if (labels is not null)
                foreach (Label label in labels)
                    tags.AddLabelTag(label);

            if (hashTags is not null)
                foreach (string hashTag in hashTags)
                    tags.AddTTag(hashTag);

            if (customEmojis is not null)
                foreach (Emoji? emoji in customEmojis.Where(x => x.IsCustom && (x.EmojifiedName is not null && content.Contains(x.EmojifiedName))))
                    tags.AddEmojiTag(emoji.UnemojifiedName ?? "", emoji.Url);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            if (communityDefinitionEvent is not null && communityDefinitionEvent.Coordinates.StartsWith(NKind.CommunityDefinition.ToString()))
                tags.AddATag(communityDefinitionEvent.Coordinates, communityDefinitionEvent.PreferredRelay, communityDefinitionEvent.CustomMarker);

            return new NEvent(NKind.ShortTextNote, tags, content);
        }

        /// <summary>
        /// Generate a Kind 30023 or 30024
        /// </summary>
        /// <param name="title"></param>
        /// <param name="markdownContent"></param>
        /// <param name="imageUrl"></param>
        /// <param name="summary"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="isDraft"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="articleIdentifier"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? LongFormContent(string title, string markdownContent, string? imageUrl = null, string? summary = null,
            List<PTag>? profileMentions = null, List<PTag>? eventMentions = null, bool isDraft = false, Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, string? articleIdentifier = null, DateTime? expiration = null,
            NProxy? proxy = null, ATag? communityDefinitionEvent = null)
        {
            if (string.IsNullOrEmpty(title))
                return null;

            List<NTag> tags = new();
            tags.AddPublishedAtTag(DateTime.UtcNow);
            tags.AddTitleTag(title);
            if (!string.IsNullOrEmpty(imageUrl))
                tags.AddImageTag(imageUrl);
            if (!string.IsNullOrEmpty(summary))
                tags.AddSummaryTag(summary);
            
            // Used to make this event replaceable in case of modifications
            tags.AddDTag(string.IsNullOrEmpty(articleIdentifier) ? title : articleIdentifier);

            if (profileMentions is not null)
                foreach (PTag profile in profileMentions)
                    tags.AddPTag(profile.PubKeyHex, profile.PreferredRelay);

            if (eventMentions is not null)
                foreach (PTag ev in eventMentions)
                    tags.AddETag(ev.PubKeyHex, ev.PreferredRelay);

            if (contentWarning is not null)
                tags.AddContentWarningTag(contentWarning.Reason);

            if (labels is not null)
                foreach (Label label in labels)
                    tags.AddLabelTag(label);

            if (hashTags is not null)
                foreach (string hashTag in hashTags)
                    tags.AddTTag(hashTag);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            if (communityDefinitionEvent is not null && communityDefinitionEvent.Coordinates.StartsWith(NKind.CommunityDefinition.ToString()))
                tags.AddATag(communityDefinitionEvent.Coordinates, communityDefinitionEvent.PreferredRelay, communityDefinitionEvent.CustomMarker);

            return new NEvent(isDraft ? NKind.DraftLongFormContent : NKind.LongFormContent, tags, markdownContent);
        }


        /// <summary>
        /// Generate a Kind 6 event
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? BoostTextNote(NEvent referencedEvent, string eventPreferredRelayUrl = "", ATag? communityDefinitionEvent = null)
        {
            return BoostTextNote(referencedEvent, new ATag(referencedEvent, eventPreferredRelayUrl, (NMarkers?)null), eventPreferredRelayUrl, communityDefinitionEvent);
        }
        /// <summary>
        /// Generate a Kind 6 event
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="eventATag"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? BoostTextNote(NEvent referencedEvent, ATag eventATag, string eventPreferredRelayUrl = "", ATag? communityDefinitionEvent = null)
        {
            return ReferenceTextEvent(referencedEvent, NMarkers.mention, eventATag, NKind.Reserved, null, null, null, eventPreferredRelayUrl, null, null, null, null, null, null, communityDefinitionEvent);
        }

        /// <summary>
        /// Generate a Kind 1 event with an internal reference to another event.
        /// NOTE: the content should have a valid event identifier inside of it (see NIP21 at https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/21.md)
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="content"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="customEmojis"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? QuoteTextNote(NEvent referencedEvent, string? content, List<PTag>? profileMentions = null,
            List<PTag>? eventMentions = null, string eventPreferredRelayUrl = "", Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, List<Emoji>? customEmojis = null,
            ATag? communityDefinitionEvent = null)
        {
            return QuoteTextNote(referencedEvent, content, new ATag(referencedEvent, eventPreferredRelayUrl, (NMarkers?)null), profileMentions, eventMentions, eventPreferredRelayUrl, contentWarning, labels, hashTags, customEmojis, communityDefinitionEvent);
        }
        /// <summary>
        /// Generate a Kind 1 event with an internal reference to another event.
        /// NOTE: the content should have a valid event identifier inside of it (see NIP21 at https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/21.md)
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="content"></param>
        /// <param name="eventATag"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="customEmojis"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? QuoteTextNote(NEvent referencedEvent, string? content, ATag eventATag, List<PTag>? profileMentions = null,
            List<PTag>? eventMentions = null, string eventPreferredRelayUrl = "", Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, List<Emoji>? customEmojis = null,
            ATag? communityDefinitionEvent = null)
        {
            return ReferenceTextEvent(referencedEvent, NMarkers.mention, eventATag, NKind.ShortTextNote, content, profileMentions, eventMentions, eventPreferredRelayUrl, contentWarning, labels, hashTags, customEmojis, null, null, communityDefinitionEvent);
        }
        /// <summary>
        /// Generate a Kind 16 event.
        /// This event is used to reference any kind of events, EXCEPT kind 1.
        /// This method will return null if you pass a kind 1 on referencedEvent parameter
        /// </summary>
        /// <param name="referencedEvent">Can be any event kind, EXCEPT kind 1</param>
        /// <param name="content"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="customEmojis"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? GenericRepost(NEvent referencedEvent, string? content, List<PTag>? profileMentions = null,
            List<PTag>? eventMentions = null, string eventPreferredRelayUrl = "", Motivation? contentWarning = null, List<Label>? labels = null,
            List<string>? hashTags = null, List<Emoji>? customEmojis = null, ATag? communityDefinitionEvent = null
            )
        {
            return GenericRepost(referencedEvent, content, new ATag(referencedEvent, eventPreferredRelayUrl, (NMarkers?)null), profileMentions, eventMentions, eventPreferredRelayUrl, contentWarning, labels, hashTags, customEmojis, communityDefinitionEvent);
        }
        /// <summary>
        /// Generate a Kind 16 event.
        /// This event is used to reference any kind of events, EXCEPT kind 1.
        /// This method will return null if you pass a kind 1 on referencedEvent parameter
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="content"></param>
        /// <param name="eventATag"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="customEmojis"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        public static NEvent? GenericRepost(NEvent referencedEvent, string? content, ATag eventATag, List<PTag>? profileMentions = null,
            List<PTag>? eventMentions = null, string eventPreferredRelayUrl = "", Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, List<Emoji>? customEmojis = null, ATag? communityDefinitionEvent = null
            )
        {
            if (referencedEvent.Kind == NKind.ShortTextNote)
                return null;
            return ReferenceTextEvent(referencedEvent, NMarkers.mention, eventATag, NKind.GenericRepost, content, profileMentions, eventMentions, eventPreferredRelayUrl, contentWarning, labels, hashTags, customEmojis, null, null, communityDefinitionEvent);
        }
        /// <summary>
        /// Create an event to reference another one. The possible Kinds are 1, 6 and 16
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="eventMarker">
        ///     - "root" when the referenced event is the first of a chain
        ///     - "reply" when the referenced event is a comment of a previous event in the chain
        ///     - "mention" when using this method for Reposts or Quotes
        /// </param>
        /// <param name="referencedEventATag"></param>
        /// <param name="kind"></param>
        /// <param name="content"></param>
        /// <param name="profileMentions"></param>
        /// <param name="eventMentions"></param>
        /// <param name="eventPreferredRelayUrl"></param>
        /// <param name="contentWarning"></param>
        /// <param name="labels"></param>
        /// <param name="hashTags"></param>
        /// <param name="customEmojis"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <param name="communityDefinitionEvent"></param>
        /// <returns></returns>
        private static NEvent? ReferenceTextEvent(NEvent referencedEvent, NMarkers eventMarker, ATag referencedEventATag, NKind kind, string? content,
            List<PTag>? profileMentions = null, List<PTag>? eventMentions = null, string eventPreferredRelayUrl = "", Motivation? contentWarning = null,
            List<Label>? labels = null, List<string>? hashTags = null, List<Emoji>? customEmojis = null, DateTime? expiration = null,
            NProxy? proxy = null, ATag? communityDefinitionEvent = null)
        {
            if (kind != NKind.ShortTextNote && kind != NKind.GenericRepost && kind != NKind.Reserved)
                return null;

            if (string.IsNullOrEmpty(referencedEvent.Id) || string.IsNullOrEmpty(referencedEvent.PubKey))
                return null;

            string? dTagContent = null;
            if (referencedEvent.Kind == NKind.DraftLongFormContent
                || referencedEvent.Kind == NKind.LongFormContent)
            {
                List<NTag> dTags = referencedEvent.GetTagsByKey(TagKeys.ReplaceableIdentifier);
                dTagContent = dTags.Any() ? dTags.First().Value : null;
            }

            List<NTag> tags = new();
            tags.AddETag(referencedEvent.Id, eventPreferredRelayUrl, eventMarker.ToString());
            tags.AddATag(referencedEventATag.Coordinates, referencedEventATag.PreferredRelay, referencedEventATag.CustomMarker);
            tags.AddPTag(referencedEvent.PubKey);

            // Inoltre l'evento deve anche contenere tutti i valori dei TagKeys.ProfileIdentifier tag contenuti nell'evento referenziato
            foreach (NTag ptag in referencedEvent.GetTagsByKey(TagKeys.ProfileIdentifier))
                if (!string.IsNullOrEmpty(ptag.Value))
                    tags.AddPTag(ptag.Value, ptag.SecondValue, ptag.ThirdValue, ptag.FourthValue);

            // E se presente riporta anche il tag TagKeys.SubjectIdentifier
            NTag? subject = referencedEvent.GetTagsByKey(TagKeys.SubjectIdentifier).FirstOrDefault();
            if (subject is not null && !string.IsNullOrEmpty(subject.Value))
                tags.AddSubjectTag(subject.Value);

            // In caso di GenericRepost devo aggiungere un tag TagKeys.GenericRepostIdentifier
            if (kind == NKind.GenericRepost)
                tags.AddKTag(referencedEvent.Kind);

            // In caso di Boost, devo inserire la stringa JSON della nota referenziata nel Content
            if (kind == NKind.Reserved)
                content = JsonConvert.SerializeObject(referencedEvent, SerializerCustomSettings.Settings);

            // Se ci sono dei riferimenti a profili, note, o altro
            if (profileMentions is not null)
                foreach (PTag profile in profileMentions)
                    tags.AddPTag(profile.PubKeyHex, profile.PreferredRelay);
            if (eventMentions is not null)
                foreach (PTag ev in eventMentions)
                    tags.AddETag(ev.PubKeyHex, ev.PreferredRelay, eventMarker.ToString());

            if (contentWarning is not null)
                tags.AddContentWarningTag(contentWarning.Reason);

            if (labels is not null)
                foreach (Label label in labels)
                    tags.AddLabelTag(label);

            if (hashTags is not null)
                foreach (string hashTag in hashTags)
                    tags.AddTTag(hashTag);

            // Eventuali emoji
            if (customEmojis is not null)
                foreach (Emoji? emoji in customEmojis.Where(x => x.IsCustom && (x.EmojifiedName is not null && content.Contains(x.EmojifiedName))))
                    tags.AddEmojiTag(emoji.UnemojifiedName, emoji.Url);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            if (communityDefinitionEvent is not null && communityDefinitionEvent.Coordinates.StartsWith(NKind.CommunityDefinition.ToString()))
                tags.AddATag(communityDefinitionEvent.Coordinates, communityDefinitionEvent.PreferredRelay, communityDefinitionEvent.CustomMarker);

            return new NEvent(kind, tags, content);
        }


        /// <summary>
        /// NIP25: Reactions
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/25.md
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <returns></returns>
        public static NEvent LikeOrUpvote(NEvent referencedEvent)
        {
            return Reaction(referencedEvent, new Emoji("+"));
        }
        /// <summary>
        /// NIP25: Reactions
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/25.md
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <returns></returns>
        public static NEvent DislikeOrDownvote(NEvent referencedEvent)
        {
            return Reaction(referencedEvent, new Emoji("-"));
        }
        /// <summary>
        /// NIP25: Reactions
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/25.md
        /// </summary>
        /// <param name="referencedEvent"></param>
        /// <param name="emoji"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent Reaction(NEvent referencedEvent, Emoji emoji, NProxy? proxy = null)
        {
            List<NTag> tags = new();

            foreach (NTag ptag in referencedEvent.GetTagsByKey(TagKeys.EventIdentifier))
                tags.AddETag(ptag.Value, ptag.SecondValue, ptag.ThirdValue, ptag.FourthValue);

            // L'ultimo tag TagKeys.EventIdentifier è l'id della nota a cui si reagisce
            tags.AddETag(referencedEvent.Id);

            foreach (NTag ptag in referencedEvent.GetTagsByKey(TagKeys.ProfileIdentifier))
                tags.AddPTag(ptag.Value, ptag.SecondValue, ptag.ThirdValue, ptag.FourthValue);
            // L'ultimo tag TagKeys.ProfileIdentifier è la pubkey della nota a cui si reagisce
            tags.AddPTag(referencedEvent.PubKey);

            if (emoji.IsCustom)
                tags.AddEmojiTag(emoji.UnemojifiedName, emoji.Url);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.Reaction, tags, emoji.EmojifiedName);
        }


        /// <summary>
        /// NIP58: Badges
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/58.md
        /// </summary>
        /// <param name="uniqueBadgeIdentifier"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="image"></param>
        /// <param name="thumbnails"></param>
        /// <returns></returns>
        public static NEvent? BadgeDefinition(string uniqueBadgeIdentifier, string? name = null, string? description = null,
            ImageTag? image = null, List<ImageTag>? thumbnails = null)
        {
            if (string.IsNullOrEmpty(uniqueBadgeIdentifier))
                return null;

            List<NTag> tags = new();
            tags.AddDTag(uniqueBadgeIdentifier);

            if (!string.IsNullOrEmpty(name))
                tags.AddNameTag(name);
            if (!string.IsNullOrEmpty(description))
                tags.AddDescriptionTag(description);
            if (image is not null && !string.IsNullOrEmpty(image.ImageUrl))
                tags.AddImageTag(image.ImageUrl, image.ImageWidthHeightInPx);
            if (thumbnails is not null)
                foreach (ImageTag thumb in thumbnails)
                    tags.AddThumbTag(thumb.ImageUrl, thumb.ImageWidthHeightInPx);

            return new NEvent(NKind.BadgeDefinition, tags);
        }
        /// <summary>
        /// NIP58: Badges
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/58.md
        /// </summary>
        /// <param name="badgeDefinitionEvent"></param>
        /// <param name="pubkeysAwarded"></param>
        /// <returns></returns>
        public static NEvent? BadgeAward(ATag badgeDefinitionEvent, List<PTag> pubkeysAwarded)
        {
            if (pubkeysAwarded.Count == 0)
                return null;

            List<NTag> tags = new();

            if (badgeDefinitionEvent.Coordinates.StartsWith(NKind.BadgeDefinition.ToString()))
                tags.AddATag(badgeDefinitionEvent.Coordinates, badgeDefinitionEvent.PreferredRelay, badgeDefinitionEvent.CustomMarker);

            foreach (PTag p in pubkeysAwarded)
                tags.AddPTag(p.PubKeyHex, p.PreferredRelay);

            return new NEvent(NKind.BadgeAward, tags);
        }
        /// <summary>
        /// NIP58: Badges
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/58.md
        /// </summary>
        /// <param name="badgeDefinitionEvent"></param>
        /// <param name="badgeAwardEvent"></param>
        /// <returns></returns>
        public static NEvent? ProfileBadges(List<ATag> badgeDefinitionEvent, List<NEvent> badgeAwardEvent)
        {
            if (badgeDefinitionEvent.Count != badgeAwardEvent.Count)
                return null;

            List<NTag> tags = new();
            tags.AddDTag("profile_badges");
            for (int i = 0; i < badgeDefinitionEvent.Count; i++)
            {
                if (badgeDefinitionEvent[i].Coordinates.StartsWith(NKind.BadgeDefinition.ToString()))
                    tags.AddATag(badgeDefinitionEvent[i].Coordinates, badgeDefinitionEvent[i].PreferredRelay, badgeDefinitionEvent[i].CustomMarker);
                tags.AddETag(badgeAwardEvent[i].Id);
            }


            return new NEvent(NKind.ProfileBadges, tags);
        }


        /// <summary>
        /// NIP31: Labeling
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/32.md
        /// </summary>
        /// <param name="labelInfo"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent Label(Label labelInfo, DateTime? expiration = null, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddLabelTag(labelInfo);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.Label, tags, labelInfo.Description);
        }


        /// <summary>
        /// NIP38: User Status
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/38.md
        /// </summary>
        /// <param name="statusInfo"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent Status(Status statusInfo, DateTime? expiration = null, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddDTag(statusInfo.Type);

            if (!string.IsNullOrEmpty(statusInfo.Url))
                tags.AddExternalLinkTag(statusInfo.Url);
            if (!string.IsNullOrEmpty(statusInfo.Profile))
                tags.AddPTag(statusInfo.Profile);
            if (!string.IsNullOrEmpty(statusInfo.EventId))
                tags.AddETag(statusInfo.EventId);
            if (!string.IsNullOrEmpty(statusInfo.EventCoordinates))
                tags.AddATag(statusInfo.EventCoordinates);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.UserStatus, tags, statusInfo.Content);
        }


        /// <summary>
        /// NIP57: Zaps
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/57.md
        /// </summary>
        /// <param name="satoshisAmount"></param>
        /// <param name="bech32LNURL"></param>
        /// <param name="recipientHexPubKey"></param>
        /// <param name="relayUrlForZapReceipt"></param>
        /// <param name="message"></param>
        /// <param name="eventId"></param>
        /// <param name="aTag"></param>
        /// <returns></returns>
        public static NEvent? ZapRequest(decimal satoshisAmount, string bech32LNURL, string recipientHexPubKey, List<string> relayUrlForZapReceipt,
            string? message, string? eventId, ATag? aTag)
        {
            List<NTag> tags = new();

            tags.AddPreferredRelayTag(relayUrlForZapReceipt);
            tags.AddAmountTag(satoshisAmount);
            tags.AddLNURLTag(bech32LNURL);
            tags.AddPTag(recipientHexPubKey);

            if (!string.IsNullOrEmpty(eventId))
                tags.AddETag(eventId);

            if (aTag is not null || !string.IsNullOrEmpty(aTag.Coordinates))
                tags.AddATag(aTag.Coordinates, aTag.PreferredRelay, aTag.CustomMarker);
            return new NEvent(NKind.ZapRequest, tags, message);
        }


        /// <summary>
        /// NIP47: Nostr Wallet Connect
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/47.md
        /// </summary>
        /// <param name="walletConnect"></param>
        /// <param name="invoiceLN"></param>
        /// <returns></returns>
        public static NEvent? WalletRequestPayment(WalletConnect walletConnect, string invoiceLN)
        {
            return WalletRequestPayment(walletConnect, WalletRequest.CreatePayInvoiceRequest(invoiceLN));
        }
        /// <summary>
        /// NIP47: Nostr Wallet Connect
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/47.md
        /// </summary>
        /// <param name="walletConnect"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static NEvent? WalletRequestPayment(WalletConnect walletConnect, WalletRequest parameters)
        {
            if (walletConnect.WalletNSec is null)
                return null;
            List<NTag> tags = new();
            tags.AddPTag(walletConnect.WalletPubkey);
            NEvent ev = new NEvent(NKind.WalletRequest, tags, JsonConvert.SerializeObject(parameters, SerializerCustomSettings.Settings));
            if (!ev.Encrypt(walletConnect.WalletNSec))
                return null;
            return ev;
        }
        public static async Task<NEvent?> WalletRequestPayment(WalletConnect walletConnect, WalletRequest parameters, Func<byte[], byte[], Task<string?>> overrideEncryptionMethod)
        {
            if (walletConnect.WalletNSec is null)
                return null;
            List<NTag> tags = new();
            tags.AddPTag(walletConnect.WalletPubkey);
            NEvent ev = new NEvent(NKind.WalletRequest, tags, JsonConvert.SerializeObject(parameters, SerializerCustomSettings.Settings));
            if (!await ev.Encrypt(walletConnect.WalletNSec, overrideEncryptionMethod))
                return null;
            return ev;
        }

        /// <summary>
        /// NIP75: Zap Goals
        /// reference: https://github.com/nostr-protocol/nips/blob/01b6bfc28666db4b259556bf55c9269ca0c059d5/75.md
        /// </summary>
        /// <param name="goalDescription"></param>
        /// <param name="satoshisAmount"></param>
        /// <param name="relaysList"></param>
        /// <param name="beneficiaries"></param>
        /// <param name="closedAt"></param>
        /// <param name="externalLink"></param>
        /// <param name="referencedReplaceableEvent"></param>
        /// <returns></returns>
        public static NEvent? ZapGoal(string goalDescription, decimal satoshisAmount, List<string> relaysList,
            List<ZapTag> beneficiaries, DateTime? closedAt = null, string? externalLink = null, ATag? referencedReplaceableEvent = null)
        {
            List<NTag> tags = new();

            tags.AddAmountTag(satoshisAmount);
            tags.AddPreferredRelayTag(relaysList);
            if (closedAt is not null)
                tags.AddClosedAtTag(closedAt.Value);
            if (!string.IsNullOrEmpty(externalLink))
                tags.AddExternalLinkTag(externalLink);
            if (referencedReplaceableEvent is not null && !string.IsNullOrEmpty(referencedReplaceableEvent.Coordinates))
                tags.AddATag(referencedReplaceableEvent.Coordinates, referencedReplaceableEvent.PreferredRelay, referencedReplaceableEvent.CustomMarker);

            if (beneficiaries is not null)
                foreach (ZapTag beneficiary in beneficiaries)
                    tags.AddZapTag(beneficiary.HexPubKey, beneficiary.MetadataRelayUrl, beneficiary.Weight);

            return new NEvent(NKind.ZapGoal, tags, goalDescription);
        }


        /// <summary>
        /// NIP28: Public Chat (channels)
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/28.md
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent CreateChannel(ChannelMetadata metadata, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);
            return new NEvent(NKind.ChannelCreation, tags, JsonConvert.SerializeObject(metadata, SerializerCustomSettings.Settings));
        }
        /// <summary>
        /// NIP28: Public Chat (channels)
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/28.md
        /// </summary>
        /// <param name="newMetadata"></param>
        /// <param name="channelCreationKind40Event"></param>
        /// <param name="preferredRelayUrl"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent UpdateChannelMetadata(ChannelMetadata newMetadata, NEvent channelCreationKind40Event, string? preferredRelayUrl,
            NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddETag(channelCreationKind40Event.Id, preferredRelayUrl);
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);
            return new NEvent(NKind.ChannelMetadata, tags, JsonConvert.SerializeObject(newMetadata, SerializerCustomSettings.Settings));
        }
        /// <summary>
        /// NIP28: Public Chat (channels)
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/28.md
        /// </summary>
        /// <param name="content"></param>
        /// <param name="marker"></param>
        /// <param name="channelCreationKind40Event"></param>
        /// <param name="rootMessageKind42Event"></param>
        /// <param name="channelCreationKind40EventPreferredRelayUrl"></param>
        /// <param name="rootMessageKind42EventPreferredRelayUrl"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent CreateChannelMessage(string content, NMarkers marker, NEvent channelCreationKind40Event,
            NEvent? rootMessageKind42Event, string? channelCreationKind40EventPreferredRelayUrl, string? rootMessageKind42EventPreferredRelayUrl,
            DateTime? expiration = null, NProxy? proxy = null)
        {
            List<NTag> tags = new();

            tags.AddETag(channelCreationKind40Event.Id, channelCreationKind40EventPreferredRelayUrl);
            if (marker == NMarkers.reply)
                tags.AddETag(rootMessageKind42Event.Id, rootMessageKind42EventPreferredRelayUrl);

            foreach (NTag ptag in channelCreationKind40Event.GetTagsByKey(TagKeys.ProfileIdentifier))
                tags.AddPTag(ptag.Value, ptag.SecondValue, ptag.ThirdValue, ptag.FourthValue);
            foreach (NTag ptag in rootMessageKind42Event.GetTagsByKey(TagKeys.ProfileIdentifier))
                if (!tags.Any(x => x.Key == TagKeys.ProfileIdentifier && x.Value == ptag.Value))
                    tags.AddPTag(ptag.Value, ptag.SecondValue, ptag.ThirdValue, ptag.FourthValue);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.ChannelMessage, tags, content);
        }
        /// <summary>
        /// NIP28: Public Chat (channels)
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/28.md
        /// </summary>
        /// <param name="motivation"></param>
        /// <param name="kind42EventToHide"></param>
        /// <param name="preferredRelayUrl"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent HideChannelMessage(Motivation motivation, NEvent? kind42EventToHide, string? preferredRelayUrl, DateTime? expiration = null,
            NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddETag(kind42EventToHide.Id, preferredRelayUrl);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.ChannelHideMessage, tags, JsonConvert.SerializeObject(motivation, SerializerCustomSettings.Settings));
        }
        /// <summary>
        /// NIP28: Public Chat (channels)
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/28.md
        /// </summary>
        /// <param name="motivation"></param>
        /// <param name="pubkeyToMute"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent MuteChannelUser(Motivation motivation, string pubkeyToMute, DateTime? expiration = null, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddPTag(pubkeyToMute);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.ChannelMuteUser, tags, JsonConvert.SerializeObject(motivation, SerializerCustomSettings.Settings));
        }


        /// <summary>
        /// NIP72: Moderated communities
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/72.md
        /// </summary>
        /// <param name="uniqueCommunityName"></param>
        /// <param name="moderators"></param>
        /// <param name="relays"></param>
        /// <param name="description"></param>
        /// <param name="summary"></param>
        /// <param name="title"></param>
        /// <param name="image"></param>
        /// <param name="thumbnails"></param>
        /// <returns></returns>
        public static NEvent? CommunityDefinition(string uniqueCommunityName, List<PTag> moderators, List<RelayTag>? relays = null,
            string? description = null, string? summary = null, string? title = null, ImageTag? image = null, List<ImageTag>? thumbnails = null)
        {
            if (string.IsNullOrEmpty(uniqueCommunityName) || moderators is null || moderators.Count == 0)
                return null;

            List<NTag> tags = new();

            tags.AddDTag(uniqueCommunityName);

            foreach (PTag mod in moderators)
                tags.AddPTag(mod.PubKeyHex, mod.PreferredRelay, string.IsNullOrEmpty(mod.Role) ? "moderator" : mod.Role);

            if (!string.IsNullOrEmpty(description))
                tags.AddDescriptionTag(description);
            if (!string.IsNullOrEmpty(summary))
                tags.AddSummaryTag(summary);
            if (!string.IsNullOrEmpty(title))
                tags.AddTitleTag(title);

            if (image is not null && !string.IsNullOrEmpty(image.ImageUrl))
                tags.AddImageTag(image.ImageUrl, image.ImageWidthHeightInPx);
            if (thumbnails is not null)
                foreach (ImageTag thumb in thumbnails)
                    tags.AddThumbTag(thumb.ImageUrl, thumb.ImageWidthHeightInPx);

            if (relays is not null)
                foreach (RelayTag relay in relays)
                    tags.AddRelayTag(relay.RelayUri, relay.CustomMarker);

            return new NEvent(NKind.CommunityDefinition, tags);
        }
        /// <summary>
        /// NIP72: Moderated communities
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/72.md
        /// </summary>
        /// <param name="communityDefinitionEvent"></param>
        /// <param name="approvedEventETag"></param>
        /// <param name="approvedEventAuthorPTag"></param>
        /// <param name="originalPostRequest"></param>
        /// <returns></returns>
        public static NEvent? CommunityPostApproval(ATag communityDefinitionEvent, ETag approvedEventETag,
            PTag approvedEventAuthorPTag, NEvent originalPostRequest)
        {
            if (communityDefinitionEvent is null || string.IsNullOrEmpty(communityDefinitionEvent.Coordinates) || !communityDefinitionEvent.Coordinates.StartsWith(NKind.CommunityDefinition.ToString())
                || approvedEventETag is null || string.IsNullOrEmpty(approvedEventETag.EventId)
                || approvedEventAuthorPTag is null || string.IsNullOrEmpty(approvedEventAuthorPTag.PubKeyHex)
                || originalPostRequest is null || originalPostRequest.Kind == NKind.CommunityDefinition)
                return null;

            List<NTag> tags = new();

            tags.AddATag(communityDefinitionEvent.Coordinates, communityDefinitionEvent.PreferredRelay);
            tags.AddETag(approvedEventETag.EventId, approvedEventETag.PreferredRelay);
            tags.AddPTag(approvedEventAuthorPTag.PubKeyHex, approvedEventAuthorPTag.PreferredRelay);
            tags.AddKTag(originalPostRequest.Kind);

            return new NEvent(NKind.CommunityPostApproval, tags, JsonConvert.SerializeObject(originalPostRequest, SerializerCustomSettings.Settings));
        }


        /// <summary>
        /// NIP56: Reporting
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/56.md
        /// </summary>
        /// <param name="reportedPubKey"></param>
        /// <param name="reportedNoteId"></param>
        /// <param name="message"></param>
        /// <param name="reportType"></param>
        /// <param name="additionalLabels"></param>
        /// <returns></returns>
        public static NEvent Report(string reportedPubKey, string? reportedNoteId, string? message, NReportType reportType, List<Label>? additionalLabels)
        {
            List<NTag> tags = new();
            tags.AddPTag(reportedPubKey, reportType.ToString());
            if (!string.IsNullOrEmpty(reportedNoteId))
                tags.AddETag(reportedNoteId, reportType.ToString());

            if (additionalLabels is not null)
                foreach (Label label in additionalLabels)
                    tags.AddLabelTag(label);

            return new NEvent(NKind.Reporting, tags is null ? new() : (List<NTag>)tags, message);
        }


        /// <summary>
        /// NIP04: Encrypted Direct Message
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/04.md
        /// </summary>
        public static NEvent? EncryptedDirectMessage(string content, NPub receiver, NSec sender, NProxy? proxy = null)
        {
            List<NTag> tags = new();

            tags.AddPTag(receiver.Hex);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            NEvent ev = new NEvent(NKind.EncryptedDm, tags, content);
            if (!ev.Encrypt(sender))
                return null;
            return ev;
        }
        public static async Task<NEvent?> EncryptedDirectMessage(string content, NPub receiver, NSec sender, Func<byte[], byte[], Task<string?>> overrideEncryptionMethod, NProxy? proxy = null)
        {
            List<NTag> tags = new();

            tags.AddPTag(receiver.Hex);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            NEvent ev = new NEvent(NKind.EncryptedDm, tags, content);
            if (!await ev.Encrypt(sender, overrideEncryptionMethod))
                return null;
            return ev;
        }


        /// <summary>
        /// NIP15: Marketplace
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/15.md
        /// </summary>
        /// <param name="stall"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent MarketplaceStall(Stall stall, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);
            tags.AddDTag(stall.Id);

            return new NEvent(NKind.SetStall, tags);
        }
        /// <summary>
        /// NIP15: Marketplace
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/15.md
        /// </summary>
        /// <param name="product"></param>
        /// <param name="categories"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent MarketplaceProduct(Product product, List<string> categories, NProxy? proxy = null)
        {
            List<NTag> tags = new();

            tags.AddDTag(product.Id);
            foreach (string category in categories)
                tags.AddTTag(category);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.SetProduct, tags);
        }
        /// <summary>
        /// NIP15: Marketplace
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/15.md
        /// </summary>
        /// <param name="order"></param>
        /// <param name="sender"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent? MarketplaceCustomerOrder(CustomerOrder order, NPub receiver, NSec sender, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return EncryptedDirectMessage(JsonConvert.SerializeObject(order, SerializerCustomSettings.Settings), receiver, sender);
        }
        /// <summary>
        /// NIP15: Marketplace
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/15.md
        /// </summary>
        /// <param name="paymentRequest"></param>
        /// <param name="sender"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent? MarketplacePaymentRequest(PaymentRequest paymentRequest, NPub receiver, NSec sender, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return EncryptedDirectMessage(JsonConvert.SerializeObject(paymentRequest, SerializerCustomSettings.Settings), receiver, sender);
        }
        /// <summary>
        /// NIP15: Marketplace
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/15.md
        /// </summary>
        /// <param name="orderStatusUpdate"></param>
        /// <param name="sender"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent? MarketplaceOrderStatusUpdate(OrderStatusUpdate orderStatusUpdate, NPub receiver, NSec sender, NProxy? proxy = null)
        {
            List<NTag> tags = new();
            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return EncryptedDirectMessage(JsonConvert.SerializeObject(orderStatusUpdate, SerializerCustomSettings.Settings), receiver, sender);
        }


        /// <summary>
        /// NIP52: Calendar Events
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/52.md
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="location"></param>
        /// <param name="geohash"></param>
        /// <param name="participants"></param>
        /// <param name="hashTags"></param>
        /// <param name="externalLinks"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent? DateBasedCalendar(Guid uniqueId, string name, string? description, DateTime start, DateTime? end,
            string? location = null, string? geohash = null, List<string>? participants = null, List<string>? hashTags = null,
            List<string>? externalLinks = null, DateTime? expiration = null, NProxy? proxy = null)
        {
            if (end.HasValue && start > end)
                return null;

            List<NTag> tags = new();
            tags.AddDTag(uniqueId.ToString());
            tags.AddNameTag(name);
            tags.AddStartDateTag(start);
            if (end.HasValue)
                tags.AddEndDateTag(end.Value);
            if (!string.IsNullOrEmpty(location))
                tags.AddLocationTag(location);
            if (!string.IsNullOrEmpty(geohash))
                tags.AddGTag(geohash);
            if (participants is not null)
                foreach (string participant in participants)
                    tags.AddPTag(participant);
            if (hashTags is not null)
                foreach (string hashtag in hashTags)
                    tags.AddTTag(hashtag);
            if (externalLinks is not null)
                foreach (string link in externalLinks)
                    tags.AddExternalLinkTag(link);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.DateBasedCalendarEvent, tags, description);
        }
        /// <summary>
        /// NIP52: Calendar Events
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/52.md
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="location"></param>
        /// <param name="geohash"></param>
        /// <param name="participants"></param>
        /// <param name="hashTags"></param>
        /// <param name="externalLinks"></param>
        /// <param name="expiration"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static NEvent? TimeBasedCalendar(Guid uniqueId, string name, string? description, DateTime start, DateTime? end,
            string? location, string? geohash, List<string>? participants, List<string>? hashTags, List<string>? externalLinks,
            DateTime? expiration = null, NProxy? proxy = null)
        {
            if (end.HasValue && start > end)
                return null;

            List<NTag> tags = new();
            tags.AddDTag(uniqueId.ToString());
            tags.AddNameTag(name);
            tags.AddStartTimeTag(start);
            //tags.AddStartTimezoneTag(/* TODO: ottenere il timezone nel formato del db IANA */);
            if (end.HasValue)
            {
                tags.AddEndTimeTag(end.Value);
                //tags.AddEndTimezoneTag(/* TODO: ottenere il timezone nel formato del db IANA */);
            }
            if (!string.IsNullOrEmpty(location))
                tags.AddLocationTag(location);
            if (!string.IsNullOrEmpty(geohash))
                tags.AddGTag(geohash);
            if (participants is not null)
                foreach (string participant in participants)
                    tags.AddPTag(participant);
            if (hashTags is not null)
                foreach (string hashtag in hashTags)
                    tags.AddTTag(hashtag);
            if (externalLinks is not null)
                foreach (string link in externalLinks)
                    tags.AddExternalLinkTag(link);

            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.TimeBasedCalendarEvent, tags, description);
        }
        /// <summary>
        /// NIP52: Calendar Events
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/52.md
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dateOrTimeBasedCalendarEvents"></param>
        /// <returns></returns>
        public static NEvent? Calendar(string name, List<ATag> dateOrTimeBasedCalendarEvents)
        {
            List<NTag> tags = new();
            tags.AddNameTag(name);

            foreach (ATag calEvent in dateOrTimeBasedCalendarEvents)
                tags.AddATag(calEvent.Coordinates, calEvent.PreferredRelay, calEvent.CustomMarker);

            return new NEvent(NKind.Calendar, tags);
        }
        /// <summary>
        /// NIP52: Calendar Events
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/52.md
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <param name="dateOrTimeBasedCalendarEvent"></param>
        /// <param name="status"></param>
        /// <param name="freeBusy"></param>
        /// <returns></returns>
        public static NEvent? CalendarRSVP(Guid uniqueId, ATag dateOrTimeBasedCalendarEvent, NCalendarRSVPStatus status, NCalendarRSVPFreeBusy? freeBusy)
        {
            List<NTag> tags = new();
            tags.AddATag(dateOrTimeBasedCalendarEvent.Coordinates, dateOrTimeBasedCalendarEvent.PreferredRelay, dateOrTimeBasedCalendarEvent.CustomMarker);
            tags.AddDTag(uniqueId.ToString());

            tags.AddLabelTag(new("status", status.ToString()));
            if (freeBusy.HasValue && status != NCalendarRSVPStatus.declined)
                tags.AddLabelTag(new("freebusy", freeBusy.ToString()));

            return new NEvent(NKind.CalendarRSVP, tags);
        }


        /// <summary>
        /// NIP53: Live Activities
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/53.md
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <param name="title"></param>
        /// <param name="summary"></param>
        /// <param name="previewImageUrl"></param>
        /// <param name="streamingUrl"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="liveStatus"></param>
        /// <param name="participants"></param>
        /// <param name="totalParticipants"></param>
        /// <param name="hashTags"></param>
        /// <param name="recordingUrl"></param>
        /// <param name="preferredRelays"></param>
        /// <returns></returns>
        public static NEvent LiveActivity(Guid uniqueId, string title, string summary, string previewImageUrl,
            string streamingUrl, DateTime start, DateTime? end, NLiveEventStatus liveStatus,
            List<NLiveEventParticipant> participants, int totalParticipants,
            List<string>? hashTags, string? recordingUrl, List<string>? preferredRelays)
        {
            List<NTag> tags = new();
            tags.AddDTag(uniqueId.ToString());
            tags.AddTitleTag(title);
            tags.AddSummaryTag(summary);
            tags.AddImageTag(previewImageUrl);
            tags.AddStatusTag(liveStatus);
            tags.AddStreamingTag(streamingUrl);
            if (!string.IsNullOrEmpty(recordingUrl))
                tags.AddRecordingTag(recordingUrl);

            tags.AddStartAtTag(start);
            if (end.HasValue)
                tags.AddEndAtTag(end.Value);

            tags.AddCurrentParticipantsTag(participants.DistinctBy(x => x.Pubkey).Count());
            tags.AddTotalParticipantsTag(totalParticipants);

            if (hashTags is not null)
                foreach (string hashtag in hashTags.Distinct())
                    tags.AddTTag(hashtag);

            foreach (NLiveEventParticipant participant in participants.DistinctBy(x => x.Pubkey))
                tags.AddPTag(participant.Pubkey, participant.RelayUri, participant.StringifiedRole, participant.Proof);

            if (preferredRelays is not null)
                tags.AddPreferredRelayTag(preferredRelays);

            return new NEvent(NKind.LiveEvent, tags);
        }
        /// <summary>
        /// NIP53: Live Activities
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/53.md
        /// </summary>
        /// <param name="message"></param>
        /// <param name="liveEvent"></param>
        /// <returns></returns>
        public static NEvent LiveChat(string message, ATag liveEvent)
        {
            List<NTag> tags = new();
            tags.AddATag(liveEvent.Coordinates, liveEvent.PreferredRelay, string.IsNullOrEmpty(liveEvent.CustomMarker) ? NMarkers.root.ToString() : liveEvent.CustomMarker);

            return new NEvent(NKind.LiveChatMessage, tags, message);
        }


        /// <summary>
        /// NIP09: Event Deletion
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/09.md
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="optionalExplanation"></param>
        /// <returns></returns>
        public static NEvent Deletion(NEvent ev, string optionalExplanation = "", NProxy? proxy = null)
        {
            List<NTag> tags = new();
            tags.AddETag(ev.Id);

            // Se presenti, aggiungo anche i tag a (NIP33)
            foreach (NTag dTag in ev.GetTagsByKey(TagKeys.ReplaceableIdentifier))
                if (!string.IsNullOrEmpty(dTag.Value))
                    tags.AddATag(ev.GetCoordinates());

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(NKind.EventDeletion, tags, optionalExplanation);
        }
        /// <summary>
        /// NIP09: Event Deletion
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/09.md
        /// </summary>
        /// <param name="events"></param>
        /// <param name="optionalExplanation"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static List<NEvent> Deletion(List<NEvent> events, string optionalExplanation = "", NProxy? proxy = null)
        {
            List<NEvent> deletionEvents = new();
            foreach (IGrouping<NKind, NEvent> grp in events.GroupBy(x => x.Kind))
            {
                List<NTag> tags = new();
                foreach (NEvent ev in grp)
                {
                    tags.AddETag(ev.Id);

                    // Se presenti, aggiungo anche i tag a (NIP33)
                    foreach (NTag dTag in ev.GetTagsByKey(TagKeys.ReplaceableIdentifier))
                        if (!string.IsNullOrEmpty(dTag.Value))
                            tags.AddATag(ev.GetCoordinates());

                }

                if (proxy is not null)
                    tags.AddProxyTag(proxy.UniqueID, proxy.Type);

                deletionEvents.Add(new NEvent(NKind.EventDeletion, tags, optionalExplanation));
            }
            return deletionEvents;
        }


        /// <summary>
        /// NIP78: Arbitrary Custom App Data
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/78.md
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <param name="customContent"></param>
        /// <param name="customTags"></param>
        /// <returns></returns>
        public static NEvent? ApplicationSpecificData(string uniqueIdentifier, string? customContent, List<NTag>? customTags = null)
        {
            if (string.IsNullOrEmpty(uniqueIdentifier))
                return null;

            List<NTag> tags = new();

            tags.AddDTag(uniqueIdentifier);

            if (customTags is not null)
                tags.AddRange(customTags);

            return new NEvent(NKind.ApplicationSpecificData, tags, customContent);
        }


        /// <summary>
        /// NIP94: File Metadata
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/94.md
        /// </summary>
        /// <returns></returns>
        public static NEvent? FileMetadata(string fileUrl, string mimeType, string sha256HexEncodedFileString,
            string key, string nonce, string sizeInBytes, string sizeInPixel, string magnetLink, string torrentInfoHash,
            string blurhash)
        {
            List<NTag> tags = new();

            tags.AddUrlTag(fileUrl);
            tags.AddMTag(mimeType);
            tags.AddXTag(sha256HexEncodedFileString);
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(nonce))
                tags.AddAes256GcmTag(key, nonce);
            if (string.IsNullOrEmpty(sizeInBytes))
                tags.AddSizeTag(sizeInBytes);
            if (string.IsNullOrEmpty(sizeInPixel))
                tags.AddDimTag(sizeInPixel);
            if (string.IsNullOrEmpty(magnetLink))
                tags.AddMagnetTag(magnetLink);
            if (string.IsNullOrEmpty(torrentInfoHash))
                tags.AddITag(torrentInfoHash);
            if (string.IsNullOrEmpty(blurhash))
                tags.AddBlurhashTag(blurhash);


            return new NEvent(NKind.FileMetadata, tags);
        }


        /// <summary>
        /// NIP89: Recommended Application Handlers
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/89.md
        /// </summary>
        /// <param name="supportedKind">All the tags MUST contain a HandlerPlatform valid name in the CustomMarker property.</param>
        /// <param name="platformRecommendations"></param>
        /// <returns></returns>
        public static NEvent? HandlerRecommendations(NKind supportedKind, List<ATag> platformRecommendations)
        {
            if (supportedKind == NKind.CustomKindNotParsable
                || platformRecommendations is null || platformRecommendations.Count == 0)
                return null;

            List<NTag> tags = new();

            tags.AddDTag(((int)supportedKind).ToString());

            foreach (ATag platform in platformRecommendations)
                tags.AddATag(platform.Coordinates, platform.PreferredRelay, platform.CustomMarker);

            return new NEvent(NKind.HandlerRecommendations, tags);
        }
        /// <summary>
        /// NIP89: Recommended Application Handlers
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/89.md
        /// </summary>
        /// <param name="uniqueIdentifier"></param>
        /// <param name="supportedKinds"></param>
        /// <param name="handlers"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public static NEvent? HandlerInformations(string uniqueIdentifier, List<NKind> supportedKinds, List<CustomHandlerTag> handlers, UserMetadata? metadata = null)
        {
            if (string.IsNullOrEmpty(uniqueIdentifier)
                || supportedKinds is null || supportedKinds.Count == 0
                || handlers is null || handlers.Count == 0)
                return null;

            List<NTag> tags = new();

            tags.AddDTag(uniqueIdentifier);

            foreach (NKind kind in supportedKinds)
                tags.AddKTag(kind);

            foreach (CustomHandlerTag handler in handlers)
                tags.Add(new(handler.Type, handler.Resource, handler.Identifier));

            string? content = null;
            if (metadata is not null)
                content = JsonConvert.SerializeObject(metadata, SerializerCustomSettings.Settings);

            return new NEvent(NKind.HandlerInformations, tags, content);
        }


        /// <summary>
        /// NIP98: HTTP Auth
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/98.md
        /// </summary>
        /// <returns></returns>
        public static NEvent? HttpAuth(string absoluteUrl, string method, string? serializedRequestBody = null)
        {
            if (string.IsNullOrEmpty(absoluteUrl) || string.IsNullOrEmpty(method))
                return null;

            List<NTag> tags = new();
            tags.AddUTag(absoluteUrl);
            tags.AddMethodTag(method);

            if (!string.IsNullOrEmpty(serializedRequestBody))
                tags.AddPayloadTag(serializedRequestBody.GetSha256().ToHexString());

            return new NEvent(NKind.HttpAuth, tags);
        }


        /// <summary>
        /// NIP99: Classified Listing
        /// reference: https://github.com/nostr-protocol/nips/blob/3f218fc3a16080537405e3f712b30ef10e709d5f/99.md
        /// </summary>
        /// <returns></returns>
        public static NEvent? ClassifiedListing(
            bool isDraft,
            string title,
            string markdownDescription,
            DateTime firstPublishTime,
            int price,
            string currencyCodeISO4217,
            ClassifiedListingRecurringPrice? recurringPrice = null,
            List<string>? hashTags = null,
            List<string>? imagesUrls = null,
            string? summary = null,
            string? location = null,
            string? geohash = null,
            List<PTag>? eventMentions = null,
            List<ATag>? replaceableEventsMentions = null)
        {
            if (string.IsNullOrEmpty(title))
                return null;

            List<NTag> tags = new();

            tags.AddTitleTag(title);

            if (!string.IsNullOrEmpty(summary))
                tags.AddSummaryTag(summary);

            tags.AddPublishedAtTag(firstPublishTime);

            if (!string.IsNullOrEmpty(location))
                tags.AddLocationTag(location);

            tags.AddPriceTag(price, currencyCodeISO4217, recurringPrice);


            if (hashTags is not null)
                foreach (string hashTag in hashTags)
                    tags.AddTTag(hashTag);

            if (imagesUrls is not null)
                foreach (string imageUrl in imagesUrls)
                    tags.AddImageTag(imageUrl);

            if (!string.IsNullOrEmpty(geohash))
                tags.AddGTag(geohash);

            if (eventMentions is not null)
                foreach (PTag ev in eventMentions)
                    tags.AddETag(ev.PubKeyHex, ev.PreferredRelay);

            if (replaceableEventsMentions is not null)
                foreach (ATag ev in replaceableEventsMentions)
                    tags.AddATag(ev.Coordinates, ev.PreferredRelay, ev.CustomMarker);

            return new NEvent(isDraft ? NKind.ClassifiedListingDraft : NKind.ClassifiedListing, tags, markdownDescription);
        }


        public static NEvent CustomEvent(int customKind, string? content, List<NTag>? tags = null, DateTime? expiration = null,
            NProxy? proxy = null)
        {
            if (expiration is not null)
                tags.AddExpirationTag(expiration.Value);

            if (proxy is not null)
                tags.AddProxyTag(proxy.UniqueID, proxy.Type);

            return new NEvent(customKind, tags is null ? new() : (List<NTag>)tags, content);
        }
    }
}
