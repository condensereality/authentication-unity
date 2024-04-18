using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cdm.Authentication;
using UnityEngine;
using Cdm.Authentication.OAuth2;
using Cdm.Authentication.Utils;

public class DiscordAuth : AuthorizationCodeFlow
{
    public DiscordAuth(Configuration configuration) : base(configuration)
    {
    }
    
    public override string authorizationUrl => "https://discord.com/oauth2/authorize";
    public override string accessTokenUrl => "https://discord.com/api/oauth2/token";


    [DataContract]
    public class DiscordUserInfo : IUserInfo
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string id { get; set; }

        [DataMember(Name = "name")] 
        public string name { get; set; }

        [DataMember(Name = "email")] 
        public string email { get; set; }
        
        [DataMember(Name = "avatar")]
        public string picture { get; set; }
    }
}