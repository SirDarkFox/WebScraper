using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebScraper
{
    internal class Program
    {
        static HttpClient _client;

        //Оптимальный объём выборки данних установлен эмпирическим путём
        //Он основывается на том, что при использовании выборок меньших размеров
        //Отправка запросов на получение данных с сервера становится узким местом программы
        //И тратит гораздо больший объём времени по сравнению с обработкой и загрузкой данных в БД
        static int _sampleSize = 8000;

        static void Main(string[] args)
        {
            //Создание клиента для отправки запросов на сайт
            _client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = ~DecompressionMethods.None
            });

            //Проверка на то, что база данных существует
            //Если нет - выполняется запрос на её создание
            if (!Sql.DatabaseIsCreated())
                Sql.CreateDatabase();

            //Основной код парсера
            while (true)
            {
                Console.WriteLine($"Время начала: {DateTime.Now.ToString("HH:mm:ss")}");
                Console.WriteLine();
                //Получение выборки данных с сайта
                int pageNumber = 0;
                var content = GetContent(_sampleSize, pageNumber);

                //Обработка выборки
                while (content.Count() > 0)
                {
                    foreach (var deal in content)
                    {
                        //Проверка на валидацию
                        if (!ValidationCheck(deal))
                            continue;

                        DealModel newDeal = deal.ToObject<DealModel>();

                        //Если сделка с древесиной уже есть в базе данных
                        //И данные, хранимые в ней были изменены, то
                        //Сделка обновляется
                        if (Sql.DealExists(newDeal.DealNumber))
                        {
                            if (NeedsUpdate(newDeal, newDeal.DealNumber))
                            {
                                Sql.UpdateDeal(newDeal);
                            }
                        }
                        //Если сделки нет в базе данных, то
                        //Она добавляется
                        else
                        {
                            Sql.CreateDeal(newDeal);
                        }
                    }

                    Console.WriteLine($"Страница: {pageNumber}, Обработано: {_sampleSize * pageNumber + content.Count()}");

                    //Получение следующей выборки данных
                    pageNumber++;
                    content = GetContent(_sampleSize, pageNumber);
                }

                Console.WriteLine();
                Console.WriteLine($"Время окончания: {DateTime.Now.ToString("HH:mm:ss")}");
                Console.WriteLine();
                Console.WriteLine("Ожидание...");
                Console.WriteLine();

                //Ожидание 10 минут
                Thread.Sleep(10 * 60 * 1000);
            }
        }

        static JToken GetContent(int size, int number)
        {
            //Формирование запроса в соответствии с cURL, который был получен из сайта
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://lesegais.ru/open-area/graphql");

            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Language", "ru,en-US;q=0.9,en;q=0.8");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Cookie", "f59d54d5d8f9f615ff8dfe9216fbec16=lavll6pg6jl3hml3r140al000f; _ym_uid=1677946052862216442; _ym_d=1677946052; _ym_isad=1; _ym_visorc=w");
            request.Headers.Add("Origin", "https://lesegais.ru");
            request.Headers.Add("Pragma", "no-cache");
            request.Headers.Add("Referer", "https://lesegais.ru/open-area/deal");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36");
            request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"110\", \"Not A(Brand\";v=\"24\", \"Google Chrome\";v=\"110\"");
            request.Headers.Add("sec-ch-ua-mobile", "?1");
            request.Headers.Add("sec-ch-ua-platform", "\"Android\"");

            //Тело запроса, в котором определяется размер и номер выборки данных
            request.Content = new StringContent($"{{\"query\":\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {{\\n  searchReportWoodDeal(filter: $filter, pageable: {{number: $number, size: $size}}, orders: $orders) {{\\n    content {{\\n      sellerName\\n      sellerInn\\n      buyerName\\n      buyerInn\\n      woodVolumeBuyer\\n      woodVolumeSeller\\n      dealDate\\n      dealNumber\\n      __typename\\n    }}\\n    __typename\\n  }}\\n}}\\n\",\"variables\":{{\"size\":{size},\"number\":{number},\"filter\":null,\"orders\":null}},\"operationName\":\"SearchReportWoodDeal\"}}");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            //Отправка запроса и получение данных в формате .json
            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().Result;

            //Извлечение только необходимых данных о Сделках с Древесиной при помощи JToken
            JToken jsonModel = JsonConvert.DeserializeObject<JToken>(responseBody);
            return jsonModel["data"]["searchReportWoodDeal"]["content"];
        }

        static bool ValidationCheck(JToken deal)
        {
            //Номер декларации является уникальным идентификатом
            //Это означает, что он не может быть NULL
            //И при этом должен сотоять из комбинации цифр длинной 28 символов
            //Так как именно такой формат привелирует у 99% данных
            if ((string)deal["dealNumber"] == null)
                return false;
            else if (!Regex.IsMatch((string)deal["dealNumber"], "^[0-9]{28}$"))
                return false;


            //Наименование продавца и его ИНН всегда требуются
            //Соответственно они не могут быть NULL
            //Если продавец - Индивидуальный Предприниматель, то
            //Его наименование должно начинаться с ИП и состоять из Фамилии Имени Отчества
            //А его ИНН должен состоять из 12 цифр как у Физического лица в соответствии с законодательством РФ

            //Если продавец является Юридическим лицом, то
            //Его ИНН должен состоять из 10 цифр в соответствии с законодательством РФ
            if ((string)deal["sellerName"] == null)
                return false;
            else if ((string)deal["sellerInn"] == null)
                return false;
            else if (Regex.IsMatch((string)deal["sellerName"], "^(?=.{0,300}$)(ИП\\s[А-Я][А-Яа-я]*\\s[А-Я][А-Яа-я]*\\s[А-Я][А-Яа-я]*)$"))
            {
                if (!Regex.IsMatch((string)deal["sellerInn"], "^[0-9]{12}$"))
                {
                    return false;
                }
            }
            else if (Regex.IsMatch((string)deal["sellerName"], "^(?=.{0,300}$)([А-Яа-я\\s\"-]+)$"))
            {
                if (!Regex.IsMatch((string)deal["sellerInn"], "^[0-9]{10}$"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


            //Наименование покупателя всегда требуется
            //Соответственно оно не может быть NULL
            //Если покупатель - Индивидуальный Предприниматель, то
            //Его наименование должно начинаться с ИП и состоять из Фамилии Имени Отчества
            //А его ИНН должен состоять из 12 цифр как у Физического лица в соответствии с законодательством РФ

            //Если покупатель является Юридическим лицом, то
            //Его ИНН должен состоять из 10 цифр в соответствии с законодательством РФ

            //Если для покупателя было выбрано значение по умолчанию - Физическое лицо, то
            //ИНН может быть не указан
            if ((string)deal["buyerName"] == null)
                return false;
            else if ((string)deal["buyerInn"] == null)
                return false;
            else if (Regex.IsMatch((string)deal["buyerName"], "^(?=.{0,300}$)(ИП\\s[А-Я][А-Яа-я]*\\s[А-Я][А-Яа-я]*\\s[А-Я][А-Яа-я]*)$"))
            {
                if (!Regex.IsMatch((string)deal["buyerInn"], "^[0-9]{12}$"))
                {
                    return false;
                }
            }
            else if ((string)deal["buyerName"] == "Физическое лицо")
            {
                if ((string)deal["buyerInn"] != "")
                {
                    return false;
                }
            }
            else if (Regex.IsMatch((string)deal["buyerName"], "^(?=.{0,300}$)([А-Яа-я\\s\"-]+)$"))
            {
                if (!Regex.IsMatch((string)deal["buyerInn"], "^[0-9]{10}$"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


            //Дата сделки всегда требуется
            //Поэтому она не может быть NULL
            //Машину времени ещё не изобрели, поэтому дата сделки не может превышать сегодняшнюю
            //Для работы с SQL Server дата не может быть меньше чем '1753-01-01'
            if (!DateTime.TryParse((string)deal["dealDate"], out DateTime date))
                return false;
            else if (!((DateTime)deal["dealDate"] >= DateTime.Parse("1753-01-01")) || !((DateTime)deal["dealDate"] <= DateTime.Now.Date))
                return false;

            return true;
        }

        static bool NeedsUpdate(DealModel deal, string id)
        {
            var oldDeal = Sql.GetDealById(id);

            if (oldDeal.SellerName != deal.SellerName)
                return true;
            if (oldDeal.SellerInn != deal.SellerInn)
                return true;
            if (oldDeal.BuyerName != deal.BuyerName)
                return true;
            if (oldDeal.BuyerInn != deal.BuyerInn)
                return true;
            if (oldDeal.DealDate != deal.DealDate)
                return true;
            if (oldDeal.WoodVolumeSeller != deal.WoodVolumeSeller)
                return true;
            if (oldDeal.WoodVolumeBuyer != deal.WoodVolumeBuyer)
                return true;

            return false;
        }
    }
}
