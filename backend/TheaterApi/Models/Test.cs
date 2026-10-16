using System.ComponentModel.DataAnnotations.Schema;

namespace TheaterApi.Models;

[Table("test")]
public class Test
{
    [Column("id")]
    public int Id { get; set; }

    [Column("test_data")]
    public string TestData { get; set; } = string.Empty;
}