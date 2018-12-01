<Query Kind="Program">
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Npgsql</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Npgsql</Namespace>
</Query>

static string ConnectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
static string WavesFilePath = @"G:\michael\googledrive\documents\the-goat\goat-2018\2018-waves-list.json";
static string ResultsFilePath = @"G:\michael\googledrive\documents\the-goat\goat-2018\2018-results.json";
static int Year = DateTime.Now.Year;

void Main()
{
	var wavesFileContent = File.ReadAllText(WavesFilePath);
	var rawWaves = JsonConvert.DeserializeObject<RawWaves[]>(wavesFileContent);
	
	var db = new Db(ConnectionString);

	var entrants = new List<Entrant>();
	foreach (var rawWave in rawWaves)
	{
		var entrant = new Entrant
		{
			Bib = int.Parse(rawWave.Bib),
			Year = Year,
			FirstName = rawWave.FirstName,
			LastName = rawWave.LastName,
			EventId = db.GetEventId(rawWave.Event),
			WaveId = db.GetWaveId(rawWave.Wave),
			Completions = SanitizeCompletions(rawWave.Completions)
		};
		entrants.Add(entrant);
	}
	
	db.InsertEntrants(entrants);
	
	
	
	
	
}





class Db : IDisposable
{
	private IDbConnection Connection;
	public Db(string connectionString)
	{
		Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
		Connection = new NpgsqlConnection(connectionString);
		Connection.Open();
						
		Events = Connection.Query<Event>("select * from events");
		Waves = Connection.Query<Wave>("select * from waves");
		Divisions = Connection.Query<Division>("select * from divisions");
	}

	public void Dispose()
	{
		Connection?.Dispose();
	}

	public IEnumerable<Event> Events { get; }
	public IEnumerable<Wave> Waves { get; }
	public IEnumerable<Division> Divisions { get; }

	public int GetEventId(string rawName)
	{
		var evnt = Events.SingleOrDefault(e => string.Equals(e.RawName, rawName, StringComparison.OrdinalIgnoreCase));
		return evnt.EventId;
	}
	
	public int GetWaveId(string rawName)
	{
		while (rawName.Contains("  "))
		{
			rawName = rawName.Replace("  ", " ");
		}
		
		var wave = Waves.SingleOrDefault(w => string.Equals(w.WaveName, rawName, StringComparison.OrdinalIgnoreCase));
		return wave?.WaveId ?? Waves.Single(w => string.Equals(w.WaveName, "Unknown", StringComparison.OrdinalIgnoreCase)).WaveId;
	}
	
	public void InsertEntrants(IEnumerable<Entrant> entrants)
	{
		Connection.Execute(
			"insert into entrants (bib, year, first_name, last_name, event_id, wave_id, completions) " +
			"values (@bib, @year, @firstName, @lastName, @eventId, @waveId, @completions)",
			entrants);
	}

}

public int SanitizeCompletions(string raw)
{
	var number = Regex.Match(raw, @"\d+");
	
	return number.Success
		? int.Parse(number.Value)
		: 0;

}


class RawWaves
{ 
	[JsonProperty("fn")] public string FirstName { get; set; }
	[JsonProperty("ln")] public string LastName { get; set; }
	[JsonProperty("bib")] public string Bib { get; set; }
	[JsonProperty("csc")] public string Event { get; set; }
	[JsonProperty("Q_18905")] public string Wave { get; set; }
	[JsonProperty("dv")] public string Division { get; set; }
	[JsonProperty("Q_18906")] public string Completions { get; set; }
}

class Event
{
	public int EventId { get; set; }
	public string EventName { get; set; }	
	public string RawName { get; set; }
}

class Division
{
	public int DivisionId { get; set; }
	public string Gender { get; set; }
	public int MinAge { get; set; }
	public int MaxAge { get; set; }
	public string DivisionName { get; set; }
}

class Wave
{
	public int WaveId { get; set; }
	public string WaveName { get; set; }
}

class Entrant
{
	public int Bib { get; set; }
	public int Year { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public int EventId { get; set; }
	public int WaveId { get; set; }
	public int DivisionId { get; set; }
	public int Completions { get; set; }
	public TimeSpan FinishTime { get; set; }
}