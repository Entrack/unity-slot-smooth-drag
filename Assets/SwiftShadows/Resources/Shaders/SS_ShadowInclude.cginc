// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#include "UnityCG.cginc"

struct appdata_t {
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
};

struct v2f {
    float4 vertex   : SV_POSITION;
    fixed4 color    : COLOR;
    half2 texcoord  : TEXCOORD0;
};

sampler2D _MainTex;

v2f vert(appdata_t IN) {
    v2f OUT;
    OUT.vertex = UnityObjectToClipPos(IN.vertex);
    OUT.texcoord = IN.texcoord;
    OUT.color = IN.color;

    return OUT;
}

fixed4 frag(v2f IN) : COLOR {
    fixed4 color = IN.color;
    color.a *= tex2D(_MainTex, IN.texcoord).a;
    return color;
}