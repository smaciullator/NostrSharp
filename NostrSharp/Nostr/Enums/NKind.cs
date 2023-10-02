namespace NostrSharp.Nostr.Enums
{
    /// <summary>
    /// REGULAR EVENTS: da 1000 a 9999, ci si aspetta che vengano salvati dai relays
    /// REPLACEABLE EVENTS: da 10000 a 19999, ci si aspetta che per ogni combinazione di npub e kind, solo l'ultimo evento venga salvato dai relays (le versioni precedenti vengono scartate)
    /// EPHEMERAL EVENTS: da 20000 a 29999, ci si aspetta che NON vengano salvati dai relays
    /// PARAMETRIZED REPLACEABLE EVENTS: da 30000 a 39999, ci si aspetta che per ogni combinazione di npub, kind e tag "d", solo l'ultimo evento venga salvato dai relays (le versioni precedenti vengono scartate)
    /// </summary>
    public enum NKind : int
    {
        CustomKindNotParsable = -1,

        // Regular Events (1000-9999)
        Metadata = 0,
        ShortTextNote = 1,
        RecommendRelay = 2,
        Contacts = 3,
        EncryptedDm = 4,
        EventDeletion = 5,
        Reserved = 6,
        Reaction = 7,
        BadgeAward = 8,

        GenericRepost = 16,

        ChannelCreation = 40,
        ChannelMetadata = 41,
        ChannelMessage = 42,
        ChannelHideMessage = 43,
        ChannelMuteUser = 44,

        CommunityPostApproval = 4550,

        FileMetadata = 1063,
        LiveChatMessage = 1311,

        Reporting = 1984,
        Label = 1985,

        ZapGoal = 9041,
        ZapRequest = 9734,
        ZapReceipt = 9735,

        // Replaceable Events (10000-19999)
        MuteList = 10000,
        PinList = 10001,
        RelayListMetadata = 10002,

        WalletInfo = 13194,

        // Ephemeral Events (20000-29999)
        ClientAuthentication = 22242,
        WalletRequest = 23194,
        WalletResponse = 23195,
        NostrConnect = 24133,
        HttpAuth = 27235,

        // Parameterized Replaceable Events (30000-39999)
        CategorizedPeopleList = 30000,
        CategorizedBookmarkList = 30001,

        ProfileBadges = 30008,
        BadgeDefinition = 30009,

        SetStall = 30017,
        SetProduct = 30018,

        LongFormContent = 30023,
        DraftLongFormContent = 30024,

        ApplicationSpecificData = 30078,

        LiveEvent = 30311,

        UserStatus = 30315,

        ClassifiedListing = 30402,
        ClassifiedListingDraft = 30403,

        DateBasedCalendarEvent = 31922,
        TimeBasedCalendarEvent = 31923,
        Calendar = 31924,
        CalendarRSVP = 31925,

        HandlerRecommendations = 31989,
        HandlerInformations = 31990,

        CommunityDefinition = 34550,
    }
}
