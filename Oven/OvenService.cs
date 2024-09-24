namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class OvenService : IOvenService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly OvenLogic mLogic = new OvenLogic();

	/// <summary>
	/// Get oven state
	/// </summary>
	/// <returns>Returns state enum</returns>
	public State GetOvenState()
	{
		return mLogic.GetOvenState();
	}

	/// <summary>
	/// Load bread into the oven. Will only succeed if state is Loading.
	/// </summary>
	/// <param name="loadingValue">Value to load.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Load(int loadingValue)
	{
		return mLogic.Load(loadingValue);
	}

	/// <summary>
	/// Adjust oven temperature. Will only succeed if state is heating.
	/// </summary>
	/// <param name="heatingValue">Value to add.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Heat(int heatingValue)
	{
		return mLogic.Heat(heatingValue);
	}

}