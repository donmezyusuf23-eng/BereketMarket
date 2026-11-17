using System;

namespace BereketMarket;

public class Customer
{
	public int Id { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? Note { get; set; }
	public DateTime CreatedAt { get; set; }
	public decimal BalanceLira { get; set; }
}
