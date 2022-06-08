using Godot;
using System;

public class ExternalFileDialog : Control
{
	private String dir = "/External";
	private PackedScene item = ResourceLoader.Load<PackedScene>("res://FileListItem.tscn");

	private void Cancel()
	{
		QueueFree();
	}

	private void Select(String file)
	{
		GetNode<Label>("WindowDialog/Selected").Text = file;
	}

	private void Import()
	{
		String f = GetNode<Label>("WindowDialog/Selected").Text;
		if (f!="")
		{
			Node2D graph = GetParent<Node2D>();
			graph.Call("import_graphiceq", dir+"/"+f);
			Cancel();
		}
	}

	public void Initialize(String working_dir)
	{
		dir = working_dir + dir;
		Directory d = new Directory();
		if (!d.DirExists(dir))
		{
			d.MakeDir(dir);
		}

		GetNode<Button>("WindowDialog/Cancel").Connect("pressed", this, "Cancel");
		GetNode<Button>("WindowDialog/Import").Connect("pressed", this, "Import");
		GetNode<WindowDialog>("WindowDialog").Connect("popup_hide", this, "Cancel");

		GetNode<Label>("WindowDialog/Label").Text = "Files from "+dir;
		if (d.Open(dir) == Error.Ok)
		{
			d.ListDirBegin();
			String fileName = d.GetNext();
			Godot.Collections.Array files = new Godot.Collections.Array();
			VBoxContainer list = GetNode<VBoxContainer>("WindowDialog/Files/FileList");

			while(fileName!="")
			{
				if (!d.CurrentIsDir())
				{
					Button i = item.Instance<Button>();
					i.GetNode<Label>("Name").Text = fileName;
					i.GetNode<Label>("Description").Text = "";
					i.Connect("pressed", this, "Select", new Godot.Collections.Array{fileName});
					list.AddChild(i);
				}
				fileName = d.GetNext();
			}
		}
		GetNode<WindowDialog>("WindowDialog").PopupCentered();
	}
}
