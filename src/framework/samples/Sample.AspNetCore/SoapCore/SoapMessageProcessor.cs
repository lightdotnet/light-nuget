using SoapCore.Extensibility;
using System.ServiceModel.Channels;

namespace Sample.AspNetCore.SoapCore;

public class SoapMessageProcessor(ILogger<SoapMessageProcessor> logger) : ISoapMessageProcessor
{
    public async Task<Message> ProcessMessage(Message message, HttpContext context, Func<Message, Task<Message>> next)
    {
        var bufferedMessage = message.CreateBufferedCopy(int.MaxValue);
        var msg = bufferedMessage.CreateMessage();
        var reader = msg.GetReaderAtBodyContents();
        var content = reader.ReadInnerXml();

        var path = context.Request.Query["op"];

        logger.LogWarning("{path}\r\n{content}", path, content);

        //now you can inspect and modify the content at will.
        //if you want to pass on the original message, use bufferedMessage.CreateMessage(); otherwise use one of the overloads of Message.CreateMessage() to create a new message
        var newMessage = bufferedMessage.CreateMessage();

        //pass the modified message on to the rest of the pipe.
        var responseMessage = await next(newMessage);

        //Inspect and modify the contents of returnMessage in the same way as the incoming message.
        //finish by returning the modified message.	

        return responseMessage;
    }
}