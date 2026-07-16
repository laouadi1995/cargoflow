namespace CargoFlow.Api.Models;

public class StopDto
{
    public int Seq { get; set; }
    public string TrackingId { get; set; } = "";
    public string Address { get; set; } = "";
    public string RouteCode { get; set; } = "";
    public string Dimensions { get; set; } = "";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

public class AddressGroupDto
{
    /// <summary>
    /// Numéro du groupe (1, 2, 3...) basé sur le plus petit Seq du groupe
    /// </summary>
    public int GroupNumber { get; set; }

    /// <summary>
    /// Adresse de base (sans "App 101", etc.)
    /// </summary>
    public string Address { get; set; } = "";

    /// <summary>
    /// Coordonnées GPS
    /// </summary>
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    /// <summary>
    /// Tous les stops (Seq) regroupés sous cette destination
    /// </summary>
    public List<StopDto> Stops { get; set; } = new();

    /// <summary>
    /// Nombre de colis pour cette destination
    /// </summary>
    public int DeliveryCount => Stops.Count;

    /// <summary>
    /// Plage Seq pour l'affichage (ex: "1-3")
    /// </summary>
    public string SeqRange
    {
        get
        {
            var seqs = Stops.OrderBy(s => s.Seq).ToList();
            if (seqs.Count == 1)
                return seqs[0].Seq.ToString();
            return $"{seqs[0].Seq}-{seqs[seqs.Count - 1].Seq}";
        }
    }

    /// <summary>
    /// Le plus petit Seq du groupe (pour l'ordre des stops)
    /// </summary>
    public int MinSeq => Stops.Count > 0 ? Stops.Min(s => s.Seq) : 0;
}
