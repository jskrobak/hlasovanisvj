using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using hlasovanisvj.Domain;

namespace hlasovanisvj.Services;

public class DataImportService(MemberService memberService)
{
    public async Task ImportHtmlAsync(int sessionId, Stream stream)
    {
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

            switch (cols.Length)
            {
                case 2:
                {
                    var owner = new Member();

                    var parts = cols[0].TextContent.Split("Jednotka: ");

                    var nameAddressParts = parts[0].Split(", ");

                    owner.SessionId = sessionId;
                    //owner.Id = cnt;
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
                    break;
                }
                case 1:
                    currOwner.Addresses.Add(cols.First().TextContent);
                    break;
            }
        }

        if (members.Count == 0)
            throw new InvalidOperationException("V souboru nebyli nalezeni žádní vlastníci.");

        // Mazat až po úspěšném parsování, ať chybný soubor nesmaže stávající účastníky.
        // Smaže i hosty – import je určený pro přípravu schůze, ne pro její průběh.
        await memberService.ClearMembersAsync(sessionId);
        await memberService.InsertMembers(members);
    }
}