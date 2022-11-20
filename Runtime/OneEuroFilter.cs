/* 
 * OneEuroFilter.cs
 * Damon Chandler, 2022
 * 
 * Based on the 1€ filter by Géry Casiez (http://www.lifl.fr/~casiez/1euro/)
 * and the C# implementation by Dario Mazzanti (https://github.com/DarioMazzanti/OneEuroFilterUnity)
 * and the C++ implementation by Nicolas Roussel (http://www.lifl.fr/~casiez/1euro/OneEuroFilter.cc)
 * with understanding aided by Jaan Tollander de Balsch (https://jaantollander.com/post/noise-filtering-using-one-euro-filter/)
 * 
 */

namespace Nomad
{
	using System;
	using UnityEngine;

	[Serializable]
	public struct OneEuroParameters
	{
		public static readonly OneEuroParameters Default = new OneEuroParameters()
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

	[Serializable]
	public abstract class OneEuroFilterBase<T> where T : struct
	{
		[SerializeField]
		private OneEuroParameters _parameters = OneEuroParameters.Default;

		private T _currentOutput;

		public ref readonly T CurrentOutput => ref _currentOutput;
		protected abstract int _dimensions { get; }
		protected  const int foo = 1;
		private float[] _buffer;
		private float? _lastTime;
		internal readonly OneEuroFilterInternal[] _filters;

		protected OneEuroFilterBase()
		{
			_buffer = new float[_dimensions];
			_filters = new OneEuroFilterInternal[_dimensions];
			for (var i = 0; i < _dimensions; i++)
			{
				_filters[i] = new OneEuroFilterInternal(_parameters);
			}
		}

		public T Filter(in T input, float? timestamp = null) // TODO: calculate alpha once and pass to dimensional filters
		{
			// Calculate sampling frequency based on timestamps.
			if (_lastTime.HasValue && timestamp.HasValue)
			{
				_parameters.Frequency = 1.0f / (timestamp.Value - _lastTime.Value);
			}
			_lastTime = timestamp;

			Decompose(input, ref _buffer);
			for (int i = 0; i < _dimensions; i++)
			{
				_buffer[i] = _filters[i].Filter(_buffer[i], _parameters);
			}
			Recompose(_buffer, ref _currentOutput);

			return _currentOutput;
		}

		protected abstract void Decompose(in T input, ref float[] buffer);
		protected abstract void Recompose(in float[] buffer, ref T output);
	}

	[Serializable]
	public class OneEuroFilterFloat : OneEuroFilterBase<float>
	{
		protected override int _dimensions => 1;		
		protected override void Decompose(in float input, ref float[] buffer)
		{
			buffer[0] = input;
		}
		protected override void Recompose(in float[] buffer, ref float output)
		{
			output = buffer[0];
		}
	}

	[Serializable]
	public class OneEuroFilterVector2 : OneEuroFilterBase<Vector2>
	{
		protected override int _dimensions => 2;
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

	[Serializable]
	public class OneEuroFilterVector3 : OneEuroFilterBase<Vector3>
	{
		protected override int _dimensions => 3;

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

	[Serializable]
	public class OneEuroFilterVector4 : OneEuroFilterBase<Vector4>
	{
		protected override int _dimensions => 4;

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

	[Serializable]
	public class OneEuroFilterQuaternion : OneEuroFilterBase<Quaternion>
	{
		protected override int _dimensions => 4;

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
		private float _alpha;
		private float _lastOutput;

		public bool IsInitialized { get; private set; }
		public float LastInput { get; private set; }

		public LowPassFilter(float alpha = 1f, float initialValue = 0.0f)
		{
			LastInput = _lastOutput = initialValue;
			SetAlpha(alpha);
			IsInitialized = false;
		}

		public float Filter(float value, float alpha)
		{
			SetAlpha(alpha);
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
			LastInput = value;
			_lastOutput = result;
			return result;
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

	internal class OneEuroFilterInternal
	{
		private readonly LowPassFilter _x;
		private readonly LowPassFilter _dx;
		private float? _lastTime;

		/// <summary>
		/// The latest filtered value.
		/// </summary>
		public float CurrentOutput { get; private set; }

		public OneEuroFilterInternal() : this(OneEuroParameters.Default) { }
		public OneEuroFilterInternal(OneEuroParameters parameters)
		{
			_x = new LowPassFilter();
			_dx = new LowPassFilter();
			_lastTime = -1.0f;
			
			// Validate();

			CurrentOutput = 0.0f;
		}

		public float Filter(in float input, in OneEuroParameters parameters)
		{
			// estimate the current variation per second 
			float dvalue = _x.IsInitialized ? ((input - _x.LastInput) * parameters.Frequency) : 0;
			float edvalue = _dx.Filter(dvalue, GetAlpha(parameters.DerivativeCutoff, parameters.Frequency));
			// use it to update the cutoff frequency
			float cutoff = parameters.MinCutoff + parameters.Beta * Mathf.Abs(edvalue);
			// filter the given value
			CurrentOutput = _x.Filter(input, GetAlpha(cutoff, parameters.Frequency));

			return CurrentOutput;
		}

		private static float GetAlpha(float cutoff, float frequency)
		{
			var te = 1.0f / frequency;
			var tau = 1.0f / (2.0f * Mathf.PI * cutoff);
			return 1.0f / (1.0f + tau / te);
		}
	};
}