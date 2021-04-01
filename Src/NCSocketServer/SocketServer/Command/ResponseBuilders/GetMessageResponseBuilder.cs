//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Protobuf;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command.ResponseBuilders
{
    //Dated: July 20, 2011
    /// <summary>
    /// This class is responsible for providing the responses based on the command Version specified.
    /// Main role of this class is to provide the backward compatibility. As different version of command can
    /// be processed by the same server. In that case the response should be in the form understandable by the
    /// client who sent the command.
    /// 
    /// This class only processes the different versions of BulkGet command
    /// </summary>
    class GetMessageResponseBuilder : ResponseBuilderBase
    {
        public static void BuildResponse(IDictionary<string, IList<object>> getResult, int commandVersion, string requestStringId, IList serializedResponse, int commandId, long requestID,NCache nCache, string _clientID, ClientManager clientManager)

        {
            Util.KeyPackageBuilder.Cache = nCache.Cache;
            int numberOfChunks = 0;
            int sequenceId = 1;
            long requestId = Convert.ToInt64(requestStringId);

            HashVector<string, ClusteredList<List<Message>>> resultInChunks = new HashVector<string, ClusteredList<List<Message>>>();

            foreach (var pair in getResult)
            {
                var messageListChunks = Util.KeyPackageBuilder.GetMessages(pair.Value, nCache, _clientID);

                
                    if (resultInChunks.ContainsKey(pair.Key))
                    {
                        ClusteredList<List<Message>> messageList = resultInChunks[pair.Key];
                        messageList.AddRange(messageListChunks);
                    }
                    else
                    {
                        resultInChunks.Add(pair.Key, messageListChunks);
                    }

                    numberOfChunks += messageListChunks.Count;
                
            }

            GetMessageResponse getMessageResponse = new GetMessageResponse();
            if (clientManager.ClientVersion >= 5000)
            {
                Common.Util.ResponseHelper.SetResponse(getMessageResponse, requestID, commandId);
                if (resultInChunks.Count == 0)
                {
                    serializedResponse.Add(Common.Util.ResponseHelper.SerializeResponse(getMessageResponse, Common.Protobuf.Response.Type.GET_MESSAGE));
                    return;
                }
                
                foreach (var pair in resultInChunks)
                {
                    //response.sequenceId = sequenceId++;
                    TopicMessages topicMessage = new TopicMessages();
                    topicMessage.topic = pair.Key;

                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        topicMessage.messageList.AddRange(pair.Value[i]);
                    }
                    getMessageResponse.topicMessages.Add(topicMessage);
                }

              
                serializedResponse.Add(Common.Util.ResponseHelper.SerializeResponse(getMessageResponse, Common.Protobuf.Response.Type.GET_MESSAGE));
            }
            else
            {
                Response response = new Response();
                Common.Util.ResponseHelper.SetResponse(response, requestID, commandId, Common.Protobuf.Response.Type.GET_MESSAGE);
                if (resultInChunks.Count == 0)
                {
                    response.getMessageResponse = getMessageResponse;
                    serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                    return;
                }
                
                foreach (var pair in resultInChunks)
                {
                    //response.sequenceId = sequenceId++;
                    TopicMessages topicMessage = new TopicMessages();
                    topicMessage.topic = pair.Key;

                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        topicMessage.messageList.AddRange(pair.Value[i]);
                    }
                    getMessageResponse.topicMessages.Add(topicMessage);
                }

            

                response.getMessageResponse = getMessageResponse;
                serializedResponse.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
        
        }
    }
}
