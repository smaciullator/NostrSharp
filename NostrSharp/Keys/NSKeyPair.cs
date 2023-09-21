using System;

namespace NostrSharp.Keys
{
    public class NSKeyPair : IEquatable<NSKeyPair>
    {
        public NSec NSec { get; }
        public NPub NPub { get; }


        public NSKeyPair(NSec privateKey, NPub publicKey)
        {
            NSec = privateKey;
            NPub = publicKey;
        }
        public NSKeyPair(NSec privateKey)
        {
            NSec = privateKey;
            NPub = NPub.FromPrivateEc(NSec.Ec);
        }


        /// <summary>
        /// Generate a new random key pair
        /// </summary>
        public static NSKeyPair GenerateNew()
        {
            NSec privateKey = NSec.New();
            return new NSKeyPair(privateKey);
        }
        /// <summary>
        /// Create key pair based on private key
        /// </summary>
        public static NSKeyPair From(NSec privateKey)
        {
            return new NSKeyPair(privateKey);
        }


        public bool Equals(NSKeyPair? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return NSec.Equals(other.NSec) && NPub.Equals(other.NPub);
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((NSKeyPair)obj);
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(NSec, NPub);
        }
    }
}
