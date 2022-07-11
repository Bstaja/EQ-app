using Godot;
using System;

//FileDialog pentru gestionarea intrărilor/fișierelor din baza de date
public class FileDialog : Control
{
	private WindowDialog wd;
	private Button bSave;
	private Button bCancel;
	private LineEdit name;
	private LineEdit descr;
	private VBoxContainer fl;
	private Node db;
	private Node2D graph;


	private void save()
	{
		if (name.Text == "")
		{
			OS.Alert("You must enter a name!", "Invalid name");
			return;
		}

		db.Call("AddEntry", new String[]{name.Text, descr.Text, OS.GetUserDataDir()+"/temp/tempdata.txt", null});
		//Funcția de mai jos eliberează din memorie acest FileDialog
		QueueFree();
	}

	private void cancel()
	{
		QueueFree();
	}

	public override void _Ready()
	{
		wd = GetNode<WindowDialog>("WindowDialog");
		bSave = GetNode<Button>("WindowDialog/Save");
		bCancel = GetNode<Button>("WindowDialog/Cancel");
		name = GetNode<LineEdit>("WindowDialog/Name");
		descr = GetNode<LineEdit>("WindowDialog/Description");
		fl = GetNode<VBoxContainer>("WindowDialog/Files/FileList");
		db = GetParent().GetNode<Node>("Main/Database");
		graph = GetParent().GetNode<Node2D>("Main");

		bSave.Connect("pressed", this, "save");
		bCancel.Connect("pressed", this, "cancel");
		wd.Connect("popup_hide", this, "cancel");
	}
}
