using System;
using Alexa.NET;
using Newtonsoft.Json;
using Alexa.NET.Request;
using Amazon.Lambda.Core;
using Alexa.NET.Response;
using Alexa.NET.Request.Type;
using System.Collections.Generic;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace NumberGame
{
    public class Function
    {
        public async Task<string> FunctionHandler(object inputObject, ILambdaContext context)
        {
            var input = JsonConvert.DeserializeObject<SkillRequest>(inputObject.ToString());

            SkillResponse tell = null;

            ILambdaLogger log = context.Logger;
            log.LogLine($"Skill Request Object:" + JsonConvert.SerializeObject(input));

            Session session = input.Session;
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            Type requestType = input.GetRequestType();
            if (requestType == typeof(LaunchRequest))
            {
                string speech = "Welcome! Say new game to start";
                Reprompt rp = new Reprompt("Say new game to start");

                tell = ResponseBuilder.Tell(speech, session);
                tell.Response.ShouldEndSession = false;
            }
            else if (input.GetRequestType() == typeof(SessionEndedRequest))
            {
                tell = ResponseBuilder.Tell("Goodbye!");
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                switch (intentRequest.Intent.Name)
                {
                    case "AMAZON.CancelIntent":
                    case "AMAZON.StopIntent":
                        tell = ResponseBuilder.Tell("Goodbye!");
                        break;
                    case "NewGameIntent":
                        {
                            session.Attributes["num_guesses"] = 0;
                            Random rnd = new Random();
                            Int32 magicNumber = rnd.Next(1, 5);
                            session.Attributes["magic_number"] = magicNumber;

                            string next = "Guess a number between 1 and 5";
                            Reprompt rp = new Reprompt(next);
                            tell = ResponseBuilder.Ask(next, rp, session);
                        }
                        break;
                    case "AnswerIntent":
                        {
                            // check answer
                            string userString = intentRequest.Intent.Slots["Number"].Value;
                            Int32 userInt = 0;
                            Int32.TryParse(userString, out userInt);
                            bool correct = (userInt == (Int32)(long)session.Attributes["magic_number"]);
                            Int32 numTries = (Int32)(long)session.Attributes["num_guesses"] + 1;
                            string speech = "";
                            if (correct)
                            {
                                if (numTries > 1)
                                {
                                    speech = "Correct! You guessed it in " + numTries.ToString() + " tries. Say new game to play again, or stop to exit. ";
                                } else
                                {
                                    speech = "Correct! You guessed it in " + numTries.ToString() + " try. Say new game to play again, or stop to exit. ";
                                }
                                session.Attributes["num_guesses"] = 0;
                            }
                            else
                            {
                                speech = "Nope, guess again.";
                                session.Attributes["num_guesses"] = numTries;
                            }
                            Reprompt rp = new Reprompt("speech");
                            tell = ResponseBuilder.Ask(speech, rp, session);
                        }
                        break;
                    default:
                        {
                            log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                            string speech = "I didn't understand - try again?";
                            Reprompt rp = new Reprompt(speech);
                            tell = ResponseBuilder.Ask(speech, rp, session);
                        }
                        break;
                }
            }

            var x = JsonConvert.SerializeObject(tell, Formatting.None, new JsonSerializerSettings());
            return x;
        }
    }
}