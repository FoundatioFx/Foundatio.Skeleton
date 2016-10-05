using System;
using System.Text;
using Newtonsoft.Json;

namespace Foundatio.Skeleton.Domain.Models {
    public class TokenResult {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        public string Domain { get; set; }
        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        public string DecodeIdToken() {
            if (String.IsNullOrEmpty(IdToken))
                return null;

            var parts = IdToken.Split('.');
            if (parts.Length != 3) {
                throw new ArgumentException("Token must consist from 3 delimited by dot parts");
            }
            var payload = parts[1];
            return Encoding.UTF8.GetString(Base64UrlDecode(payload));

        }

        public string GetEmailFromOAuthIdToken() {
            var decodedObject = DecodeIdToken();
            if (String.IsNullOrEmpty(decodedObject))
                return null;

            var idToken = JsonConvert.DeserializeObject<OAuthIdToken>(decodedObject);
            if (String.IsNullOrEmpty(idToken?.Email))
                return null;

            return idToken.Email;
        }

        // from JWT spec
        private static byte[] Base64UrlDecode(string input) {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break;  // One pad char
                default:
                    throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }

    public class OAuthIdToken {
        public string Email { get; set; }
    }
}
