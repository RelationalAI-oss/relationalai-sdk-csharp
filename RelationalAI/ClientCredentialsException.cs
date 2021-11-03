using System;
namespace Com.RelationalAI
{

    /// <summary> Class to describe Access Token retrieval exception. </summary>
   public class ClientCredentialsException : Exception
    {
        public ClientCredentialsException() { }
        public ClientCredentialsException(string message) : base(message) { }
        public ClientCredentialsException(string message, System.Exception inner) : base(message, inner) { }
        protected ClientCredentialsException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}