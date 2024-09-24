namespace Services;

/// <summary>
/// Oven states.
/// </summary>
public enum State : int
{
	Loading,
	Heating
}


/// <summary>
/// Service contract.
/// </summary>
public interface IOvenService
{
	/// <summary>
	/// Retrieves current oven state.
	/// </summary>
	/// <returns></returns>
	State GetOvenState();

	/// <summary>
	/// Load bread into the oven. Will only succeed if state is Loading.
	/// </summary>
	/// <param name="loadingValue">Value to load.</param>
	/// <returns>True on success, false on failure.</returns>
	bool Load(int loadingValue);

	/// <summary>
	/// Adjust oven temperature. Will only succeed if state is heating.
	/// </summary>
	/// <param name="heatingValue">Value to add.</param>
	/// <returns>True on success, false on failure.</returns>
	bool Heat(int heatingValue);
}
