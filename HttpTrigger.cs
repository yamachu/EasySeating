using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

namespace EasySeating
{
    public static class HttpTrigger
    {
        [FunctionName("HttpTrigger")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("Start");
            var requestForm = req.Form;
            if (!req.Form.TryGetValue("token", out Microsoft.Extensions.Primitives.StringValues token))
            {
                log.Error("require Token");
                return new BadRequestResult();
            }

            if (token.ToString() != Settings.Instance.VerifiedToken)
            {
                log.Error("Token is invalid");
                return new BadRequestResult();
            }

            // 時間かかりそうならこっちに投げる
            if (!req.Form.TryGetValue("response_url", out Microsoft.Extensions.Primitives.StringValues response_url))
            {
                return new BadRequestResult();
            }

            var channel_id = req.Form["channel_id"];
            var user_name = req.Form["user_name"];
            log.Info($"message from {user_name} in {channel_id}");

            var client = new HttpClient();
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            log.Info("get channel info");
            var channel_info_response = client.GetAsync($"https://slack.com/api/channels.info?token={Settings.Instance.SlackAPIToken}&channel={channel_id}").Result;
            var channel_response_body = channel_info_response.Content.ReadAsStringAsync().Result;
            IEnumerable<string> member_ids;
            var json_body = JObject.Parse(channel_response_body);
            if (json_body["ok"].Value<bool>())
            {
                member_ids = json_body["channel"]["members"].Values<string>();
            }
            else
            {
                log.Info("chennel is private, use group info");
                var group_info_response = client.GetAsync($"https://slack.com/api/groups.info?token={Settings.Instance.SlackAPIToken}&channel={channel_id}").Result;
                var group_response_body = group_info_response.Content.ReadAsStringAsync().Result;
                json_body = JObject.Parse(group_response_body);
                member_ids = json_body["group"]["members"].Values<string>();
            }

            log.Info("get all users list");
            var users_response = client.GetAsync($"https://slack.com/api/users.list?token={Settings.Instance.SlackAPIToken}").Result;
            var users_response_body = users_response.Content.ReadAsStringAsync().Result;
            var members_json = JObject.Parse(users_response_body);
            var members = members_json["members"].OfType<JObject>().Select(v =>
            {
                var _id = v.Value<string>("id");
                var _is_bot = v.Value<bool>("is_bot");
                var _profile = v["profile"];
                var _real_name = _profile.Value<string>("real_name");
                var _display_name = _profile.Value<string>("display_name");

                return (_real_name, _display_name, _is_bot, _id);
            }).ToDictionary(v => v._id, v => v);

            var group_members = member_ids.Select(key => members[key]).Where(v => !v._is_bot).ToList();

            // if 3 rows, total 20 seats,
            // ex)
            // 3 5 10 5
            var text = (string)req.Form["text"];
            var texts = text.Trim().Split(' ');

            if (!(texts.Length > 1 && int.TryParse(texts[0], out int row)))
            {
                log.Error("command text format is invalid: ${text}");
                return new JsonResult(new
                {
                    text = "command text format is invalid",
                    response_type = "ephemeral"
                });
            }
            if (texts.Length <= row)
            {
                log.Error("command text format is invalid: ${text}");
                return new JsonResult(new
                {
                    text = "command text format is invalid",
                    response_type = "ephemeral"
                });
            }
            var cols = texts.Skip(1).Select(v => int.Parse(v)).ToList();
            if (cols.Sum() < group_members.Count)
            {
                log.Error("total seats is less than members: ${text}");
                return new JsonResult(new
                {
                    text = "total seats is less than members",
                    response_type = "ephemeral"
                });
            }

            var rnd = new Random();
            var randomSeatedMembers = Enumerable.Range(1, cols.Sum())
            .OrderBy(r => rnd.Next())
            .Zip(group_members, (seatIdx, member) => (seatIdx, member)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("```");

            var processedSeats = 0;
            var requiredSeatsWidth = Utils.GetRequiredWidth(group_members.Count);
            foreach (var i in cols)
            {
                sb.Append(Utils.GetNumberedSquare(i, processedSeats + 1, requiredSeatsWidth));
                processedSeats += i;
            }

            randomSeatedMembers.OrderBy(v => v.seatIdx).ToList().ForEach(v => sb.AppendLine($"{v.seatIdx}: {v.member._real_name}"));

            sb.AppendLine("```");

            return new JsonResult(new
            {
                text = sb.ToString(),
                response_type = "in_channel"
            });
        }
    }
}
