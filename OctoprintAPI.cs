using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoUploader
{
    class OctoprintAPI
    {
        public String serverAddress = "";
        public String apiKey = "";
        public System.Net.HttpStatusCode lastResultCode;

        public String UploadFile (  String file, bool selectOnUpload, bool startOnUpload )
        {
            try
            {
                var client = new RestClient(this.serverAddress);
                var request = new RestRequest("/api/files/local", Method.POST);
                request.AddHeader("X-Api-Key", this.apiKey);
                request.AddHeader("Content-Type", "multipart/form-data");
                request.AddFile("file", file, "application/octet-stream");

                if (selectOnUpload)
                    request.AddParameter("select", "true");

                if (startOnUpload)
                    request.AddParameter("print", "true");

                var response = client.Execute(request);
                this.lastResultCode = response.StatusCode;
                return response.Content;
            }
            catch ( Exception ex )
            {
                this.lastResultCode = System.Net.HttpStatusCode.ExpectationFailed;
               
            }
            return null;
        }

        public String GetVersion ()
        {
            try
            {
                var client = new RestClient(this.serverAddress);
                var request = new RestRequest("/api/version", Method.GET);
                request.AddHeader("X-Api-Key", this.apiKey);

                IRestResponse response = client.Execute(request);
                this.lastResultCode = response.StatusCode;
                return response.Content;
            }
            catch ( Exception ex )
            {
                this.lastResultCode = System.Net.HttpStatusCode.ExpectationFailed;
            }
            return null;
            //return LogRequest(client, request, response);
            
        }

        private String LogRequest(RestClient client, IRestRequest request, IRestResponse response, long durationMs=1000)
        {
            var requestToLog = new
            {
                resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = request.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                }),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = client.BuildUri(request),
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };

            String space = Environment.NewLine + Environment.NewLine;
            String output = string.Format("Request completed in {0}Request: {1}Response: {2}Content {3}",
                    durationMs + " ms" + space,
                    JsonConvert.SerializeObject(requestToLog) + space,
                    JsonConvert.SerializeObject(responseToLog) + space,
                    response.Content);

            Trace.Write(output);
            return output;
        }

    }
}
