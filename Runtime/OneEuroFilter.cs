/* 
 * OneEuroFilter.cs
 * Damon Chandler, 2022
 * 
 * Based on the 1€ filter by Géry Casiez (http://www.lifl.fr/~casiez/1euro/)
 * and the C# implementation by Dario Mazzanti (https://github.com/DarioMazzanti/OneEuroFilterUnity)
 * and the C++ implementation by Nicolas Roussel (http://www.lifl.fr/~casiez/1euro/OneEuroFilter.cc)
 */

namespace Nomad
{
	using System;
	using UnityEngine;

	[Serializable]
	public struct OneEuroFilterParameters
	{
		public static readonly OneEuroFilterParameters Default = new OneEuroFilterParameters()
		{
			Frequency = 120f,
			MinCutoff = 1f,
			Beta = 0f,
			DerivativeCutoff = 1f,
		};

		public float Frequency;
		public float MinCutoff;
		public float Beta;
		public float DerivativeCutoff;
	}

	public abstract class OneEuroFilterBase<T> where T : struct
	{
		protected T _currentOutput;
		protected T _previousOutput;

		public ref readonly T CurrentOutput => ref _currentOutput;
		public ref readonly T PreviousOutput => ref _previousOutput;
		protected abstract int _dimensions { get; }
		protected float[] _buffer;
		internal readonly OneEuroFilter[] _filters;

		public OneEuroFilterBase(in OneEuroFilterParameters parameters)
		{
			_buffer = new float[_dimensions];
			_filters = new OneEuroFilter[_dimensions];
			for (int i = 0; i < _dimensions; i++)
			{
				_filters[i] = new OneEuroFilter(parameters);
			}
		}

		public void UpdateParameters(in OneEuroFilterParameters parameters)
		{
			for (int i = 0; i < _filters.Length; i++)
			{
				_filters[i].UpdateParameters(parameters);
			}
		}

		public T Filter(in T input, float? timestamp = null)
		{
			_previousOutput = _currentOutput;

			Decompose(input, ref _buffer);
			for (int i = 0; i < _dimensions; i++)
			{
				_buffer[i] = _filters[i].Filter(_buffer[i], timestamp);
			}
			Recompose(_buffer, ref _currentOutput);

			return _currentOutput;
		}

		protected abstract void Decompose(in T input, ref float[] buffer);
		protected abstract void Recompose(in float[] buffer, ref T output);
	}

	public class OneEuroFilterFloat : OneEuroFilterBase<float>
	{
		protected override int _dimensions => 1;

		public OneEuroFilterFloat(in OneEuroFilterParameters parameters) : base(parameters) { }

		protected override void Decompose(in float input, ref float[] buffer)
		{
			buffer[0] = input;
		}
		protected override void Recompose(in float[] buffer, ref float output)
		{
			output = buffer[0];
		}
	}

	public class OneEuroFilterVector2 : OneEuroFilterBase<Vector2>
	{
		protected override int _dimensions => 2;

		public OneEuroFilterVector2(in OneEuroFilterParameters parameters) : base(parameters) { }

		protected override void Decompose(in Vector2 input, ref float[] buffer)
		{
			buffer[0] = input.x;
			buffer[1] = input.y;
		}
		protected override void Recompose(in float[] buffer, ref Vector2 output)
		{
			output.x = buffer[0];
			output.y = buffer[1];
		}
	}

	public class OneEuroFilterVector3 : OneEuroFilterBase<Vector3>
	{
		protected override int _dimensions => 3;

		public OneEuroFilterVector3(in OneEuroFilterParameters parameters) : base(parameters) { }

		protected override void Decompose(in Vector3 input, ref float[] buffer)
		{
			buffer[0] = input.x;
			buffer[1] = input.y;
			buffer[2] = input.z;
		}
		protected override void Recompose(in float[] buffer, ref Vector3 output)
		{
			output.x = buffer[0];
			output.y = buffer[1];
			output.z = buffer[2];
		}
	}

	public class OneEuroFilterVector4 : OneEuroFilterBase<Vector4>
	{
		protected override int _dimensions => 4;

		public OneEuroFilterVector4(in OneEuroFilterParameters parameters) : base(parameters) { }

		protected override void Decompose(in Vector4 input, ref float[] buffer)
		{
			buffer[0] = input.x;
			buffer[1] = input.y;
			buffer[2] = input.z;
			buffer[3] = input.w;
		}
		protected override void Recompose(in float[] buffer, ref Vector4 output)
		{
			output.x = buffer[0];
			output.y = buffer[1];
			output.z = buffer[2];
			output.w = buffer[3];
		}
	}

	public class OneEuroFilterQuaternion : OneEuroFilterBase<Quaternion>
	{
		protected override int _dimensions => 4;

		public OneEuroFilterQuaternion(in OneEuroFilterParameters parameters) : base(parameters) { }

		protected override void Decompose(in Quaternion input, ref float[] buffer)
		{
			bool invert = (Vector4.SqrMagnitude(new Vector4(_filters[0].CurrentOutput, _filters[1].CurrentOutput, _filters[2].CurrentOutput, _filters[3].CurrentOutput).normalized
					- new Vector4(input[0], input[1], input[2], input[3]).normalized) > 2);

			if (invert)
			{
				buffer[0] = -input.x;
				buffer[1] = -input.y;
				buffer[2] = -input.z;
				buffer[3] = -input.w;
			}
			else
			{
				buffer[0] = input.x;
				buffer[1] = input.y;
				buffer[2] = input.z;
				buffer[3] = input.w;
			}
		}
		protected override void Recompose(in float[] buffer, ref Quaternion output)
		{
			output.x = buffer[0];
			output.y = buffer[1];
			output.z = buffer[2];
			output.w = buffer[3];
		}
	}

	internal class LowPassFilter
	{
		private float _lastInput;
		private float _alpha;
		private float _lastOutput;

		public bool IsInitialized { get; private set; }
		public float LastInput => _lastInput;

		public LowPassFilter(float alpha, float initialValue = 0.0f)
		{
			_lastInput = _lastOutput = initialValue;
			SetAlpha(alpha);
			IsInitialized = false;
		}

		public float Filter(float value)
		{
			float result;
			if (IsInitialized)
			{
				result = _alpha * value + (1.0f - _alpha) * _lastOutput;
			}
			else
			{
				result = value;
				IsInitialized = true;
			}
			_lastInput = value;
			_lastOutput = result;
			return result;
		}

		public float FilterWithAlpha(float value, float alpha)
		{
			SetAlpha(alpha);
			return Filter(value);
		}

		public void SetAlpha(float alpha)
		{
			if (alpha <= 0)
			{
				Debug.LogError("Alpha must be greater than 0.");
				alpha = float.Epsilon;
			}
			else if (alpha > 1)
			{
				Debug.LogError("Alpha must be less than or equal to 1.");
				alpha = 1f;
			}
			_alpha = alpha;
		}
	};

	internal class OneEuroFilter
	{
		protected OneEuroFilterParameters _parameters = OneEuroFilterParameters.Default;
		internal readonly LowPassFilter _x;
		internal readonly LowPassFilter _dx;
		protected float? _lastTime;

		/// <summary>
		/// The latest filtered value.
		/// </summary>
		public float CurrentOutput { get; protected set; }
		/// <summary>
		/// The second-latest filtered value.
		/// </summary>
		public float PreviousOutput { get; protected set; }
		protected ref float _freq => ref _parameters.Frequency;
		protected ref float _minCutoff => ref _parameters.MinCutoff;
		protected ref float _b => ref _parameters.Beta;
		protected ref float _derCutoff => ref _parameters.DerivativeCutoff;

		public OneEuroFilter(in OneEuroFilterParameters parameters)
		{
			_x = new LowPassFilter(GetAlpha(_minCutoff));
			_dx = new LowPassFilter(GetAlpha(_derCutoff));

			UpdateParameters(parameters);

			_lastTime = -1.0f;

			CurrentOutput = 0.0f;
			PreviousOutput = CurrentOutput;
		}

		public void UpdateParameters(in OneEuroFilterParameters parameters)
		{
			_parameters = parameters;
			_x.SetAlpha(GetAlpha(_minCutoff));
			_dx.SetAlpha(GetAlpha(_derCutoff));
			Validate();
		}

		public float Filter(float input, float? timestamp = null)
		{
			//Validate();

			PreviousOutput = CurrentOutput;

			// update the sampling frequency based on timestamps
			if (_lastTime.HasValue && timestamp.HasValue)
			{
				_freq = 1.0f / (timestamp.Value - _lastTime.Value);
			}
			_lastTime = timestamp;
			// estimate the current variation per second 
			float dvalue = _x.IsInitialized ? ((input - _x.LastInput) * _freq) : 0;
			float edvalue = _dx.FilterWithAlpha(dvalue, GetAlpha(_derCutoff));
			// use it to update the cutoff frequency
			float cutoff = _minCutoff + _b * Mathf.Abs(edvalue);
			// filter the given value
			CurrentOutput = _x.FilterWithAlpha(input, GetAlpha(cutoff));

			return CurrentOutput;
		}

		private void Validate()
		{
			if (_freq <= 0)
			{
				Debug.LogError("Frequency must be greater than 0.");
				_freq = 0;
			}
			if (_minCutoff <= 0)
			{
				Debug.LogError("MinCutoff must be greater than 0.");
				_minCutoff = float.Epsilon;
			}
			_x.SetAlpha(GetAlpha(_minCutoff));
			_dx.SetAlpha(GetAlpha(_derCutoff));
		}

		private float GetAlpha(float cutoff)
		{
			float te = 1.0f / _freq;
			float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
			return 1.0f / (1.0f + tau / te);
		}
	};
}