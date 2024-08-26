using HtmlAgilityPack;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

public class ScrapingService
{
    private readonly string _connectionString;
    private readonly string _url = "https://tr.investing.com/indices/ise-100-components";

    // Sabit Id listesi 1'den 100'e kadar
    private readonly List<int> _fixedIds = Enumerable.Range(1, 100).ToList();

    public ScrapingService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSql");
    }

    public List<StockData> ScrapeAndInsertData()
    {
        HtmlWeb web = new HtmlWeb();
        HtmlDocument document = web.Load(_url);

        var table = document.DocumentNode.SelectSingleNode("//table");

        if (table == null)
        {
            throw new Exception("Tablo bulunamadı.");
        }

        List<StockData> scrapedData = new List<StockData>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            
            // Veritabanındaki mevcut Id'leri kontrol et
            var existingIds = new HashSet<int>();
            var checkCmd = new NpgsqlCommand("SELECT Id FROM Data;", conn);
            using (var reader = checkCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingIds.Add(reader.GetInt32(0));
                }
            }

            var rows = table.SelectNodes(".//tr");
            int fixedIdIndex = 0;

            foreach (var row in rows.Skip(1))
            {
                if (fixedIdIndex >= _fixedIds.Count)
                    break; // Sabit Id'lerin dışında veri varsa işleme devam etme

                var cells = row.SelectNodes(".//td");

                if (cells != null && cells.Count >= 9)
                {
                    var stockData = new StockData
                    {
                        Id = _fixedIds[fixedIdIndex], // Sabit Id kullanımı
                        Bos = cells[0].InnerText.Trim(),
                        Isim = cells[1].InnerText.Trim(),
                        Son = cells[2].InnerText.Trim(),
                        Yuksek = cells[3].InnerText.Trim(),
                        Dusuk = cells[4].InnerText.Trim(),
                        Fark = cells[5].InnerText.Trim(),
                        Hacim = cells[6].InnerText.Trim(),
                        FarkBos = cells[7].InnerText.Trim(),
                        Zaman = cells[8].InnerText.Trim(),
                    };

                    // Id zaten veritabanında var mı kontrol et
                    if (!existingIds.Contains(stockData.Id))
                    {
                        var insertCmd = new NpgsqlCommand(
                            @"INSERT INTO Data (Id, bos, isim, son, yuksek, dusuk, fark, hacim, farkbos, zaman) 
                              VALUES (@Id, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9);", conn);

                        insertCmd.Parameters.AddWithValue("Id", stockData.Id);
                        insertCmd.Parameters.AddWithValue("p1", stockData.Bos);
                        insertCmd.Parameters.AddWithValue("p2", stockData.Isim);
                        insertCmd.Parameters.AddWithValue("p3", stockData.Son);
                        insertCmd.Parameters.AddWithValue("p4", stockData.Yuksek);
                        insertCmd.Parameters.AddWithValue("p5", stockData.Dusuk);
                        insertCmd.Parameters.AddWithValue("p6", stockData.Fark);
                        insertCmd.Parameters.AddWithValue("p7", stockData.Hacim);
                        insertCmd.Parameters.AddWithValue("p8", stockData.FarkBos);
                        insertCmd.Parameters.AddWithValue("p9", stockData.Zaman);

                        insertCmd.ExecuteNonQuery();
                    }

                    scrapedData.Add(stockData);
                    fixedIdIndex++;
                }
            }
        }

        return scrapedData;
    }
}
