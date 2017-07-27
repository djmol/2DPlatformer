interface IGroundMovement {
	GroundType groundType { get; set; }

	void ApplyGroundEffects();
	//void RemoveGroundEffects(GroundType type);
}