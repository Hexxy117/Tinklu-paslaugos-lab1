namespace Servers;

using Microsoft.VisualBasic;
using NLog;

using Services;

/*
UŽDUOTIS.
Pavadinimas:	Duonos krosnis - Vilius Malinauskas IF-1/1
Komponentai:	Krosnis (serveris), duonos įkrovimas, kaitinimas.
Aprašymas	
Kol duona kraunama į krosnį, duonos įkrovimo komponentai atsitiktiniu būdu 
generuoja įkraunamos duonos kiekius, kurie visada teigiami. Sugeneruoti kiekiai kaupiami krosnyje. 
Kol duona kraunama kaitinimas nevyksta. Kai įkrautos duonos kiekis viršyja nustatytą, krovimas nutraukiamas
ir pereinama į kepimo etapą. Kepimo etape kaitinimo komponentai generuoja krosnies temperatūros pokyčius,
kurie gali būti neigiami. Sugeneruoti pokyčiai kaupiami krosnyje. Jeigu krosnies temperatūra yra nustatyto
intervalo ribose, kas 2 sek. duona iškepa 10%. Jeigu krosnies temperatūra viršyja pasirinkto intervalo
maksimumą, bent 4 sek. duona sudega ir viskas pradedama iš naujo. Jeigu duona iškepa, kaitinimas nutraukiamas,
5 sek. duona traukiama iš krosnies ir ištraukiama. Paskui viskas pradedama iš naujo.
*/

/// <summary>
/// Static class for constant values that are used to control oven states.
/// </summary>
public static class Consts
{
	public const int MAX_TEMP = 10;
	public const int MIN_TEMP = 0;
	public const int MAX_LOAD = 50;
}


/// <summary>
/// Oven state descriptor.
/// </summary>
public class OvenState
{
	/// <summary>
	/// Access lock.
	/// </summary>
	public readonly object AccessLock = new object();

	/// <summary>
	/// Oven state.
	/// </summary>
	public State State;

	/// <summary>
	/// Current bread amount.
	/// </summary>
	public int BreadCount = 0;

	/// <summary>
	/// Current temperature.
	/// </summary>
	public int HeatCount = 0;

	/// <summary>
	/// Current batch baking progress, bread is baked when value is 10/
	/// </summary>
	public int BakingProgress = 0;

	/// <summary>
	/// Timer to track if bread is going to burn.
	/// </summary>
	public int burnTimer = 0;

	/// <summary>
	/// Timer to track if baking will progress.
	/// </summary>
	public int bakeTimer = 0;
}


/// <summary>
/// <para>Oven logic.</para>
/// <para>Thread safe.</para>
/// </summary>
class OvenLogic
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Background task thread.
	/// </summary>
	private Thread mBgTaskThread;

	/// <summary>
	/// State descriptor.
	/// </summary>
	private OvenState mState = new OvenState();

	/// <summary>
	/// Constructor.
	/// </summary>
	public OvenLogic()
	{
		//start the background task
		mBgTaskThread = new Thread(BackgroundTask);
		mBgTaskThread.Start();
	}

	/// <summary>
	/// Get current oven state.
	/// </summary>
	/// <returns>Current oven state.</returns>				
	public State GetOvenState()
	{
		lock (mState.AccessLock)
		{
			return mState.State;
		}
	}

	/// <summary>
	/// Load bread into the oven. Will only succeed if state is Loading.
	/// </summary>
	/// <param name="load">Value to load.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Load(int load)
	{
		lock (mState.AccessLock)
		{
			mLog.Info($"Loading component is trying to load {load} bread.");
			if (mState.State != State.Loading)
			{
				mLog.Info("Loading denied because state is not loading.");
				return false;
			}
			mState.BreadCount += load;
			mLog.Info($"Loading {load} bread was succesful, a total of {mState.BreadCount} bread added.");
			return true;
		}
	}

	/// <summary>
	/// Adjust oven temperature. Will only succeed if state is heating.
	/// </summary>
	/// <param name="heat">Value to add.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Heat(int heat)
	{
		lock (mState.AccessLock)
		{
			mLog.Info($"Heating component is trying to adjust temperature by {heat} .");
			if (mState.State != State.Heating)
			{
				mLog.Info("Heating denied, because state is not heating.");
				return false;
			}
			mState.HeatCount += heat;
			mLog.Info($"Temperature adjusted by {heat}, current: {mState.HeatCount} C");
			return true;
		}
	}


	/// <summary>
	/// Background task that changes oven state, by tracking loading and heatin progress.
	/// </summary>
	public void BackgroundTask()
	{
		while (true)
		{
			//sleep for a second - for easier time keeping
			Thread.Sleep(1000);

			lock (mState.AccessLock)
			{
				// Check if loading contidition is true
				if (mState.BreadCount < Consts.MAX_LOAD)
				{
					mState.State = State.Loading;
					mLog.Info($"Current state is '{mState.State}'.");
				}
				else
				{
					// Otherwise start baking!
					mState.State = State.Heating;
					// if baking progress is 10, unloads oven and resets all variables.
					if (mState.BakingProgress >= 10)
					{
						mLog.Info($"Bread succesfully baked, unloading...");
						Thread.Sleep(5000);
						mState.BreadCount = 0;
						mState.burnTimer = 0;
						mState.bakeTimer = 0;
						mState.BakingProgress = 0;
						mState.HeatCount = 0;
						mState.State = State.Loading;

					}
					switch (mState.HeatCount)
					{
						// if temperature is below interval - reset bake and burn timer progress
						case < Consts.MIN_TEMP:
							mState.bakeTimer = 0;
							mState.burnTimer = 0;
							break;
						// if temperature is above interval - reset bake timer progress, start counting burning time
						case > Consts.MAX_TEMP:
							mState.bakeTimer = 0;
							mState.burnTimer += 1;
							mLog.Info($"Bread has been burning for {mState.burnTimer}s");
							// if temperature is above interval for 4s, the bread burns and the process restarts
							if (mState.burnTimer >= 4)
							{
								mLog.Info($"Bread burned :( restarting...");
								mState.BreadCount = 0;
								mState.burnTimer = 0;
								mState.BakingProgress = 0;
								mState.HeatCount = 0;
							}
							break;
						// if temperature is in interval - reset burn timer progress, start counting baking time
						default:
							mState.burnTimer = 0;
							mState.bakeTimer += 1;
							// if temperature is in interval for 2s, adjust baking progress
							if (mState.bakeTimer >= 2)
							{
								mState.BakingProgress += 1;
								mLog.Info($"Baking progress : {mState.BakingProgress * 10} %");
								mState.bakeTimer = 0;
							}
							break;
					}
					mLog.Info($"Current state is '{mState.State}'.");
				}
			}
		}
	}
}