{{- define "startracker.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "startracker.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{- define "startracker.labels" -}}
app.kubernetes.io/name: {{ include "startracker.name" . }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "startracker.apiSecretName" -}}
{{- if .Values.api.secret.existingSecretName -}}
{{- .Values.api.secret.existingSecretName -}}
{{- else -}}
{{- printf "%s-api" (include "startracker.fullname" .) -}}
{{- end -}}
{{- end -}}
