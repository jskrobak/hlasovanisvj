using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;

namespace hlasovanisvj.Services;

public class DataImportService(MemberService memberService)
{
    public async Task ImportHtmlAsync(Organization org, Stream stream)
    {
        await memberService.ClearMembersAsync(org.Id);
        
        var parser = new HtmlParser();
        var document = parser.ParseDocument(stream);

        var table = document.QuerySelectorAll("table").First(t => t.Attributes["summary"].Value.StartsWith("Vlastníci"));
    
        
        var members = new List<Member>();
        
        var cnt = 1;

        Member currOwner = null;
    
        foreach (var row in table.QuerySelectorAll("tr"))
        {
            //var cols = row.QuerySelectorAll("td").Select(e => e.TextContent);

            var cols = row.QuerySelectorAll("td");

            if (cols.Length == 2)
            {
                var owner = new Member();

                var parts = Regex.Split(cols[0].TextContent, @"Jednotka: ");

                var nameAddressParts = Regex.Split(parts[0], ", ");

                owner.OrganizationId = org.Id;
                owner.Id = cnt;
                owner.Name = nameAddressParts.First();
                owner.Units = parts[1].Split(",").Select(u => u.TrimStart().TrimEnd()).ToList();
                owner.ShareFraction = cols[1].TextContent;
                
                var shareParts = owner.ShareFraction.Split("/");
                owner.ShareValue = (double.Parse(shareParts[0].Trim()) / double.Parse(shareParts[1].Trim()))*100.0;
                
                if (nameAddressParts.Length > 1)
                {
                    owner.Addresses.Add(nameAddressParts[1]);
                }
            
                members.Add(owner);
                
                currOwner = owner;
            
                cnt++;
            }

            if (cols.Length == 1)
            {
                currOwner.Addresses.Add(cols.First().TextContent);
            }
        }

        await memberService.InsertMembers(members);
    }
}