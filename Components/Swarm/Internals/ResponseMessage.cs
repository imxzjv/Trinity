using System;
using System.Collections.Generic;

namespace Trinity.Components.Swarm.Internals
{
    [Serializable]
    public class ResponseMessage
    {        
        public Identity From { get; set; }
        public bool Result { get; set; }
        public string ResponseText { get; set; }
        public List<RemoteClient> Clients { get; set; }
    }
}

