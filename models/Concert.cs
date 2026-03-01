using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Concert
{
    public int ConcertId { get; set; }
    public string ArtistName { get; set; }
    public string Venue { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } // For the concert poster
}