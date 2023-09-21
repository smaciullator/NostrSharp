using NostrSharp.Nostr.Enums;

namespace NostrSharp.Nostr.Models.Tags
{
    public class CustomHandlerTag
    {
        public string Type { get; set; }
        public string? Resource { get; set; }
        public string? Identifier { get; set; } = null;


        public CustomHandlerTag() { }
        private CustomHandlerTag(string type, string? resource = null, string? identifier = null)
        {
            Type = type;
            Resource = resource;
            Identifier = identifier;
        }


        /// <summary>
        /// Il secondo parametro, "resource", se è un link deve avere la string "<bech32>" come placeholder per i clients
        /// che interagiranno con l'evento per sostituirlo con la relativa risorsa da gestire
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static CustomHandlerTag AddProfileHandler(HandlerPlatform type, string? resource = null)
        {
            return new(type.ToString(), resource, Bech32Identifiers.NProfile);
        }
        /// <summary>
        /// Il secondo parametro, "resource", se è un link deve avere la string "<bech32>" come placeholder per i clients
        /// che interagiranno con l'evento per sostituirlo con la relativa risorsa da gestire
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static CustomHandlerTag AddEventHandler(HandlerPlatform type, string? resource = null)
        {
            return new(type.ToString(), resource, Bech32Identifiers.NEvent);
        }
        /// <summary>
        /// Il secondo parametro, "resource", se è un link deve avere la string "<bech32>" come placeholder per i clients
        /// che interagiranno con l'evento per sostituirlo con la relativa risorsa da gestire
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static CustomHandlerTag AddRelayHandler(HandlerPlatform type, string? resource = null)
        {
            return new(type.ToString(), resource, Bech32Identifiers.NRelay);
        }
        /// <summary>
        /// Il secondo parametro, "resource", se è un link deve avere la string "<bech32>" come placeholder per i clients
        /// che interagiranno con l'evento per sostituirlo con la relativa risorsa da gestire
        /// </summary>
        /// <param name="type"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static CustomHandlerTag AddAddressHandler(HandlerPlatform type, string? resource = null)
        {
            return new(type.ToString(), resource, Bech32Identifiers.NAddr);
        }
    }
}
