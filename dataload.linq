<Query Kind="Program">
  <NuGetReference>Dapper</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Npgsql</NuGetReference>
  <Namespace>Dapper</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Npgsql</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

static string ConnectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
static string WavesFilePath = Path.Combine(Environment.GetEnvironmentVariable("USER_PROJECTS_DIR"), @"goat-2018\2018-waves-list.json");
static string ResultsFilePath = Path.Combine(Environment.GetEnvironmentVariable("USER_PROJECTS_DIR"), @"goat-2018\2018-results.json");
static int Year = DateTime.Now.Year;

void Main()
{
	var db = new Db(ConnectionString);

	// create entrants
	var wavesFileContent = File.ReadAllText(WavesFilePath);
	var rawWaves = JsonConvert.DeserializeObject<RawWave[]>(wavesFileContent);
	
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
	
	// update with results
	var resultsFileContent = File.ReadAllText(ResultsFilePath);
	var rawResults = JsonConvert.DeserializeObject<RawResults>(resultsFileContent);
	
	foreach (var rawResult in rawResults.Data.GoatRun1)
	{
		if (rawResult[0] != rawResult[2]) throw new Exception("Bad data!");

		var bib = int.Parse(rawResult[0]);

		var entrant = entrants.FirstOrDefault(e => e.Bib == bib);
		
		if (entrant == null)
		{
			// late entrants... have to assume event, wave, and completions.
			entrant = new Entrant
			{
				Bib = bib,
				Year = Year,
				FirstName = rawResult[3].Split(' ')[0],
				LastName = rawResult[3].Split(' ')[1],
				EventId = db.GetEventId("GoatT"),
				WaveId = db.GetWaveId("unknown"),
				Completions = 0,
			};
			entrants.Add(entrant);
		}

		entrant.FinishPosition = int.Parse(rawResult[1].TrimEnd('.', ' ')); // looks like "123."
		entrant.DivisionId = db.GetDivisionId(rawResult[4]);
		entrant.FinishTime = TimeSpan.ParseExact(rawResult[5], @"hh\:mm\:ss", CultureInfo.CurrentCulture);
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
	
		Connection.Execute("truncate table entrants");
		
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
		var evnt = Events.First(e => string.Equals(e.RawName, rawName, StringComparison.OrdinalIgnoreCase));
		return evnt.EventId;
	}
	
	public int GetWaveId(string rawName)
	{
		if (rawName.Length >= 5)
		{
			var id = rawName[5].ToString();
			var wave = Waves.FirstOrDefault(w => string.Equals(w.WaveId.ToString(), id, StringComparison.OrdinalIgnoreCase));
			if (wave != null)
			{
				return wave.WaveId;
			}
		}
		
		return Waves.First(w => string.Equals(w.WaveName, "Unknown", StringComparison.OrdinalIgnoreCase)).WaveId;
	}
	
	public int GetDivisionId(string rawName)
	{
		var div = Divisions.First(d => string.Equals(d.DivisionName, rawName, StringComparison.OrdinalIgnoreCase));
		return div.DivisionId;
	}
	
	public void InsertEntrants(IEnumerable<Entrant> entrants)
	{
		var tx = Connection.BeginTransaction();
		try
		{
			Connection.Execute(
				"insert into entrants (bib, year, first_name, last_name, event_id, wave_id, division_id, completions, finish_position, finish_time) " +
				"values (@bib, @year, @firstName, @lastName, @eventId, @waveId, @divisionId, @completions, @finishPosition, @finishTime)",
				entrants, transaction: tx);
			tx.Commit();
		}
		catch (Exception)
		{
			tx.Rollback(); 
			throw;
		}
	}
	

}

public int SanitizeCompletions(string raw)
{
	var number = Regex.Match(raw, @"\d+");
	
	return number.Success
		? int.Parse(number.Value) - 1 // format is like "2nd Goat" so should be "1" completion
		: 0;

}


class RawWave
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
	public int? DivisionId { get; set; }
	public int Completions { get; set; }
	public int? FinishPosition { get; set; }
	public TimeSpan? FinishTime { get; set; }
}

class RawResults
{
	[JsonProperty("data")] public Data Data { get; set; }
}


class Data
{
	[JsonProperty("#1_Goat Run")] public string[][] GoatRun1 { get; set; }
}

