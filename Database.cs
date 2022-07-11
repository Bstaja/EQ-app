using Godot;
using System;
using LiteDB;

//Baza de date este un obiect de tip Node (un obiect simplu în Godot)
public class Database : Node
{
	//Am creat o clasă care definește o intrare în baza de date
	//Fiecare item are un id unic, un nume, o descriere, un fișier și o icoană
	//Icoanele nu au fost implementate încă, deci nu vor fi folosite
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

	//Am folosit o bază de date LiteDB, am găsit-o la întâmplare pe internet
	private LiteDatabase db;
	private ILiteStorage<int> storage;
	private ILiteCollection<Entry> col;
	//Baza de date cuprinde colecții și un mediu de stocare
	public void InitializeDatabase(String working_dir)
	{
		db = new LiteDatabase(working_dir+"/Database.db");
		col = db.GetCollection<Entry>("Entries");
		storage =  db.GetStorage<int>();
	}

	//Funcție pentru adăugarea unei noi intrări în baza de date
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
		//Fișierul asociat intrării are id-ul cu 200 mai mare decât id-ul intrării, prin urmare
		//	numărul maxim de grafice ce pot fi înregistrate în baza de date este de 200
		E.IconId = E.Id+200;
		col.Update(E);

		storage.Upload(E.FileId, F);
		//storage.Upload(E.IconId, I);
	}

	//Ștergerea unei intrări din baza de date după id-ul unic
	public void DeleteEntryId(int Id)
	{
		Entry E = col.FindById(Id);
		storage.Delete(E.FileId);
		//storage.Delete(E.IconId);
		col.Delete(Id);
	}

	//Obținerea tuturor graficelor din baza de date
	public Godot.Collections.Array<Godot.Collections.Array> GetEntries()
	{
		col.EnsureIndex("Name");
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

	//Descărcarea unui fișier din baza de date într-o locație temporară
	public void DownloadGraph(int id, String function)
	{
		Entry E = col.FindById(id);
		LiteFileInfo<int> f = storage.FindById(E.FileId);
		f.SaveAs(OS.GetUserDataDir()+"/temp/tempdata.txt");
		GetParent().Set("last_name", E.Name);
		GetParent().Call(function, OS.GetUserDataDir()+"/temp/tempdata.txt");
		
	}
}
