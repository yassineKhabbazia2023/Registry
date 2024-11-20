using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;

namespace WebApi.Configurations
{
    public class PlainTextInputFormatter : TextInputFormatter
    {
        public PlainTextInputFormatter()
        {
            SupportedMediaTypes.Add("application/csv"); 
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            using (var reader = new StreamReader(context.HttpContext.Request.Body, encoding))
            {
                var body = await reader.ReadToEndAsync(); 
                return await InputFormatterResult.SuccessAsync(body); 
            }
        }
    }
}
