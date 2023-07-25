using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace coauthor_networks.Controllers
{
    public class HomeController : Controller
    {
        private const string GRAPHVIZ_DOT_PATH = "C:\\Users\\Acer\\Desktop\\Graphviz\\bin\\dot.exe";

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string fio)
        {
            // Отправить запрос на получение публикаций
            var data = Uri.EscapeUriString($"universitet=0&fio={fio}&year_rel=&year_rel1=&year_rel2=&year_reg=&year_reg1=&year_reg2=&faculty=0&v_publ=0").Replace("%20", "+");
            var dataBytes = Encoding.UTF8.GetBytes(data);

            var request = (HttpWebRequest)WebRequest.Create("http://library.vstu.ru/publ_2/publ_result.php");
            request.Method = "Post";
            request.Host = "library.vstu.ru";
            request.KeepAlive = true;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
            request.Referer = "http://library.vstu.ru/publ_2/index.php?command=search2";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataBytes.Length;
            var sendStream = request.GetRequestStream();
            sendStream.Write(dataBytes, 0, dataBytes.Length);
            sendStream.Close();

            var response = HttpHelper.GetResponse(request);
            var responseBody = HttpHelper.GetResponseString(response);

            // Распарсить ответ
            var graphLines = new List<string>();
            var currentIndex = responseBody.IndexOf("<DIV CLASS='resultlist'");
            while (true)
            {
                // Выделить очередной блок <p>
                var indexStart = responseBody.IndexOf("<p>", currentIndex);
                if (indexStart == -1) break;
                indexStart += 3;
                var indexEnd = responseBody.IndexOf("</p>", currentIndex);
                var p = responseBody[indexStart..indexEnd];

                // Выделить строку с авторами
                var indexStart2 = p.IndexOf(" / ") + 3;
                var pattern = "((\\w|[А-Яа-яЁё])\\.(\\w|[А-Яа-яЁё])\\. ((\\w|[А-Яа-яЁё]))+(, )?)+";
                var iofsString = Regex.Match(p.Substring(indexStart2), pattern).Value;

                // Выделить отдельных авторов
                var iofs = iofsString.Split(", ");
                for (int i = 0; i < iofs.Length; i++)
                    for (int j = 0; j < i; j++)
                    {
                        var graphLine = $"\"{iofs[i]}\" -- \"{iofs[j]}\"";
                        var graphLineOpposite = $"\"{iofs[j]}\" -- \"{iofs[i]}\"";
                        if (!graphLines.Contains(graphLine) && !graphLines.Contains(graphLineOpposite))
                            graphLines.Add(graphLine);
                    }

                currentIndex = indexEnd + 4;
            }

            // Сформировать текстовое описание графа
            var sw = new StreamWriter("graph.txt");
            sw.WriteLine("graph {");
            foreach (var graphLine in graphLines)
                sw.WriteLine(graphLine);
            sw.WriteLine("}");
            sw.Close();

            // Преобразовать текстовое описание графа в картинку
            var startInfo = new ProcessStartInfo(GRAPHVIZ_DOT_PATH, "dot -Tpng graph.txt -o wwwroot\\graph.png");
            Process.Start(startInfo).WaitForExit();

            return View("BuildNetwork");
        }
    }
}
