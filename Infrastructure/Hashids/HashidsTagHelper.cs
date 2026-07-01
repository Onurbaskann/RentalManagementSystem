using System;
using System.Collections.Generic;
using HashidsNet;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KiraTakip.Infrastructure.Hashids
{
    [HtmlTargetElement("a", Attributes = "asp-hashid")]
    [HtmlTargetElement("form", Attributes = "asp-hashid")]
    public class HashidsTagHelper : TagHelper
    {
        private readonly IHashids _hashids;

        public HashidsTagHelper(IHashids hashids)
        {
            _hashids = hashids;
        }

        // Run before MVC AnchorTagHelper / FormTagHelper (which run at Order = 0)
        public override int Order => -1000;

        [HtmlAttributeName("asp-hashid")]
        public int? Hashid { get; set; }

        [HtmlAttributeName("asp-all-route-data", DictionaryAttributePrefix = "asp-route-")]
        public IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Hashid.HasValue)
            {
                var hashedValue = _hashids.Encode(Hashid.Value);
                RouteValues["id"] = hashedValue;

                // Remove the helper attribute from final html output
                output.Attributes.RemoveAll("asp-hashid");
            }
        }
    }
}
