namespace NostrSharp.Relay.Enums
{
    public class NMessageTypes
    {
        #region Client Types
        /// <summary>
        /// Usato dal client per filtrare i messaggi
        /// </summary>
        public const string Request = "REQ";
        public const string Count = "COUNT";
        #endregion

        #region Relay Types
        /// <summary>
        /// Il relay usa questo tipo di messaggio per notificare se ha accettato o meno un evento precedentemente ricevuto
        /// </summary>
        public const string Ok = "OK";
        /// <summary>
        /// Il relay notifica di aver terminato di inviare i vecchi eventi e che da ora in poi arriveranno solamente
        /// i nuovi eventi in tempo reale
        /// </summary>
        public const string Eose = "EOSE";
        /// <summary>
        /// Usato dal relay per notificare qualcosa in formato leggibile dalle persone
        /// </summary>
        public const string Notice = "NOTICE";
        #endregion

        #region Common
        /// <summary>
        /// Il client lo usa per pubblicare un evento
        /// Il relay lo usa per restituire un evento richiesto
        /// </summary>
        public const string Event = "EVENT";
        public const string Close = "CLOSE";
        public const string Authentication = "AUTH";
        #endregion
    }
}
