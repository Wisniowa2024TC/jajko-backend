namespace JajkoBackend.Models;

public class Discount
{
    public int Id { get; set; }
    public int ItemId { get; set; }

    public System.DateTime StartDate { get; set; }
    public System.DateTime EndDate { get; set; }

    public double Value { get; set; }
}
