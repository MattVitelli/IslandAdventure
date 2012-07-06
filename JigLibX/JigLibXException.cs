#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace JigLibX
{

    /// <summary>
    /// Thrown when an error occurs in JigLibX Physic Library.
    /// </summary>
    public class JigLibXException : Exception
    {

        public JigLibXException() : base("JigLibX Physic Library has thrown an Exception.") { }
        
        public JigLibXException(string message) : base(message){}

        public JigLibXException(string message, Exception innerException)  : base(message, innerException) { }

    }

}
