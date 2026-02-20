import { AfterViewInit, Component, ElementRef, ViewChild } from '@angular/core';
import { JsonPipe, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { finalize } from 'rxjs';
import { SkyMapComponent } from './sky/sky-map.component';

@Component({
  selector: 'app-root',
  imports: [FormsModule, JsonPipe, NgIf, SkyMapComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit {
  @ViewChild('resultPanel') private readonly resultPanel?: ElementRef<HTMLElement>;

  constructor(private readonly http: HttpClient) {}

  title = 'StarTracker';
  target = 'Polaris';
  lat = '';
  lon = '';
  atLocal = '';
  apiKey = '';
  baseUrl = this.getConfiguredBaseUrl();
  loading = false;
  error = '';
  result: PositionResult | null = null;
  skyDate = new Date();

  ngAfterViewInit(): void {
    this.updateSkyDate();
  }

  locate(): void {
    this.error = '';
    this.result = null;
    const validation = this.validateInputs();
    if (validation) {
      this.error = validation;
      return;
    }

    this.loading = true;
    this.updateSkyDate();

    const params = this.buildParams();
    const url = `${this.baseUrl}/v1/stars/${encodeURIComponent(this.target)}/position`;

    this.http.get<ApiResult>(url, { params, headers: { 'X-API-Key': this.apiKey } })
      .pipe(finalize(() => { this.loading = false; }))
      .subscribe({
        next: (data) => {
          this.result = data ? this.normalizeResult(data as ApiResult) : null;
          this.error = '';
          this.updateSkyDate();
          this.scrollToResults();
        },
        error: (err: HttpErrorResponse) => {
          this.error = this.formatError(err);
        }
      });
  }

  private buildParams(): HttpParams {
    let params = new HttpParams()
      .set('lat', this.lat)
      .set('lon', this.lon);

    if (this.atLocal) {
      const iso = new Date(this.atLocal).toISOString();
      params = params.set('at', iso);
    }

    return params;
  }

  private formatError(err: HttpErrorResponse): string {
    if (!err) return 'Unknown error';
    if (err.error?.detail) return err.error.detail;
    if (err.message) return err.message;
    return 'Request failed';
  }

  private normalizeResult(data: ApiResult): PositionResult {
    return {
      target: data.Target,
      rightAscensionDegrees: data.RightAscensionDegrees,
      declinationDegrees: data.DeclinationDegrees,
      azimuthDegrees: data.AzimuthDegrees,
      altitudeDegrees: data.AltitudeDegrees,
      guidance: data.Guidance,
      at: data.At
    };
  }

  private parseNumber(value: string): number | null {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  onTargetInput(event: Event): void {
    this.target = this.readInputValue(event);
  }

  onLatInput(event: Event): void {
    this.lat = this.readInputValue(event);
    this.updateSkyDate();
  }

  onLonInput(event: Event): void {
    this.lon = this.readInputValue(event);
    this.updateSkyDate();
  }

  onAtInput(event: Event): void {
    this.atLocal = this.readInputValue(event);
    this.updateSkyDate();
  }

  onApiKeyInput(event: Event): void {
    this.apiKey = this.readInputValue(event);
  }

  onBaseUrlInput(event: Event): void {
    this.baseUrl = this.readInputValue(event);
  }

  private validateInputs(): string | null {
    if (!this.apiKey) return 'API key is required.';
    if (this.parseNumber(this.lat) === null) return 'Latitude is required.';
    if (this.parseNumber(this.lon) === null) return 'Longitude is required.';
    if (!this.target.trim()) return 'Target is required.';
    return null;
  }

  private scrollToResults(): void {
    if (!this.resultPanel) return;
    this.resultPanel.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  private updateSkyDate(): void {
    this.skyDate = this.atLocal ? new Date(this.atLocal) : new Date();
  }

  private readInputValue(event: Event): string {
    const target = event.target as HTMLInputElement | null;
    return target?.value ?? '';
  }

  get latNumber(): number {
    return this.parseNumber(this.lat) ?? 45.0;
  }

  get lonNumber(): number {
    return this.parseNumber(this.lon) ?? -93.0;
  }

  private getConfiguredBaseUrl(): string {
    const configured = (globalThis as { __STARTRACKER_CONFIG__?: { apiBaseUrl?: string } })
      .__STARTRACKER_CONFIG__?.apiBaseUrl;
    return configured && configured.trim() ? configured.trim() : '/api';
  }

}

type PositionResult = {
  target: string;
  rightAscensionDegrees: number;
  declinationDegrees: number;
  azimuthDegrees: number;
  altitudeDegrees: number;
  guidance: string;
  at: string;
};

type ApiResult = {
  Target: string;
  RightAscensionDegrees: number;
  DeclinationDegrees: number;
  AzimuthDegrees: number;
  AltitudeDegrees: number;
  Guidance: string;
  At: string;
};
