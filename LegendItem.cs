using Godot;
using System;

public class LegendItem : Panel
{
	Vector2[] data_points;
	Godot.Collections.Dictionary data_graph;
	bool visible = true;

	private void Delete()
	{
		Visibility(false);
		QueueFree();
	}
	private void Visibility(bool set)
	{
		visible = set;
		GetTree().Root.GetNode<Node2D>("Main/Main").Update();
	}
	private void ChangeColor()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Randomize();
		GetNode<ColorRect>("Color").Color = Color.Color8((byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)));
		GetTree().Root.GetNode<Node2D>("Main/Main").Update();
	}
	public override void _Ready()
	{
		GetNode<Button>("ChangeColor").Connect("pressed", this, "ChangeColor");
		GetNode<CheckBox>("bVisible").Connect("toggled", this, "Visibility");
		GetNode<Button>("bDelete").Connect("pressed", this, "Delete");
	}
}
