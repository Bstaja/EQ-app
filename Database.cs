using Godot;
using System;
using LiteDB;

public class Database : Node
{
	public class Entry
	{
		public int Id {get; set;}
		public string Name {get; set;}
		public string Description {get; set;}
		public int FileId {get; set;}
		public int IconId {get; set;}

		public override string ToString()
		{
			String s = "{\n";
			s+= "\tId: " + Id.ToString() + "\n";
			s+= "\tName: " + Name + "\n";
			s+= "\tDescr: " + Description +"\n}";
			return s;
		}
	}
	private LiteDatabase db;
	private ILiteStorage<int> storage;
	private ILiteCollection<Entry> col;
	public void InitializeDatabase(String working_dir)
	{
		db = new LiteDatabase(working_dir+"/Database.db");
		col = db.GetCollection<Entry>("Entries");
		storage =  db.GetStorage<int>();
	}

	public void AddEntry(String N, String D, String F, String I = null)
	{
		Entry E = new Entry
		{
			Name = N,
			Description = D,
			FileId = 0,
			IconId = 0
		};
		col.Insert(E);
		E.FileId = E.Id;
		E.IconId = E.Id+200;
		col.Update(E);
		if (F!=null)
		{
			storage.Upload(E.FileId, F);
			//storage.Upload(E.IconId, I);
		}
	}

	public void DeleteEntryId(int Id)
	{
		Entry E = col.FindById(Id);
		storage.Delete(E.FileId);
		//storage.Delete(E.IconId);
		col.Delete(Id);
	}

	public Godot.Collections.Array<Godot.Collections.Array> GetEntries()
	{
		col.EnsureIndex("Name");
		//Entry E = col.FindOne("$.Name = 'mere'");
		Entry[] All = col.Query().ToArray();
		Godot.Collections.Array<Godot.Collections.Array> Arr = new Godot.Collections.Array<Godot.Collections.Array>();

		foreach(Entry E in All)
		{
			Godot.Collections.Array Ent = new Godot.Collections.Array();
			Ent.Add(E.Name);
			Ent.Add(E.Description);
			Ent.Add(E.Id);
			Arr.Add(Ent);
		}
		return Arr;
	}

	public void DownloadGraph(int id, String function)
	{
		Entry E = col.FindById(id);
		LiteFileInfo<int> f = storage.FindById(E.FileId);
		f.SaveAs(OS.GetUserDataDir()+"/temp/tempdata.txt");
		GetParent().Set("last_name", E.Name);
		GetParent().Call(function, OS.GetUserDataDir()+"/temp/tempdata.txt");
		
	}

    public override void _Ready()
    {
		//InitializeDatabase(OS.GetSystemDir(OS.SystemDir.Desktop));
		//AddEntry("pere", "aha");
		//DeleteEntryId(2);
		//GD.Print(GetEntries());
    }
}
