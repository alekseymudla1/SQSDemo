using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Internal;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SQSDemo
{
    public class Function
    {
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            
        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<List<string>> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            context.Logger.LogLine($@"Messages count: {evnt.Records.Count()}");
            var result = new List<string>();
            foreach(var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
                result.Add(message.Body);
            }

            return result;
        }

        private async Task<bool> ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message {message.Body}");
            var request = new PutObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("S3LogBucket"),
                Key = Guid.NewGuid().ToString(),
                ContentBody = message.Body
            };

            using (var s3 = new AmazonS3Client(RegionEndpoint.USEast1))
            {
                var response = await s3.PutObjectAsync(request);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    context.Logger.Log($"{response.HttpStatusCode} encountered putting: {request.BucketName}:{request.Key}");
                    return false;
                }
            }
        }
    }
}
