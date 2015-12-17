﻿Shader "EVE/RealWindows" {
	Properties{
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_IVATex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Clarity("Clarity", Float) = 1.1
		_Shininess("Shininess", Float) = 0
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		float4 scrPos : TEXCOORD1;
	};

	sampler2D _MainTex;
	sampler2D _IVATex;
	float4 _MainTex_ST;
	float _Clarity;
	float _Shininess;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.scrPos = ComputeScreenPos(o.vertex);
		o.scrPos.xy /= o.scrPos.w;
		return o;
	}

	fixed4 frag(v2f i) : COLOR
	{
		fixed4 tex = tex2D(_MainTex, i.texcoord);
		fixed4 iva = tex2D(_IVATex, i.scrPos.xy);
		iva.a = lerp(0, iva.a, saturate( tex.a*_Clarity));
	return iva;
	}
		ENDCG
	}
	}
}