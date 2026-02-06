import { AfterViewInit, Component, ElementRef, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';

@Component({
  selector: 'app-sky-map',
  standalone: true,
  template: '<div #celestialHost id="celestial-map" class="sky-map-host"></div>',
  styles: [
    `
      .sky-map-host {
        width: 100%;
        height: 100%;
        position: relative;
        overflow: hidden;
      }

      .sky-map-host canvas,
      .sky-map-host container,
      .sky-map-host svg {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        display: block;
      }

      .sky-map-host #celestial-zoomin,
      .sky-map-host #celestial-zoomout,
      .sky-map-host #error {
        display: none;
      }
    `
  ]
})
export class SkyMapComponent implements AfterViewInit, OnChanges {
  @ViewChild('celestialHost', { static: true }) private readonly celestialHost!: ElementRef<HTMLDivElement>;
  @Input({ required: true }) lat = 0;
  @Input({ required: true }) lon = 0;
  @Input({ required: true }) date!: Date;
  @Input() highlight = '';

  private initialized = false;

  constructor(private readonly host: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    this.renderMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) return;
    if (changes['lat'] || changes['lon'] || changes['date'] || changes['highlight']) {
      this.updateView();
    }
  }

  private renderMap(): void {
    const celestial = (window as any).Celestial;
    if (!celestial) return;
    this.celestialHost.nativeElement.innerHTML = '';
    const width = this.celestialHost.nativeElement.clientWidth;
    const height = this.celestialHost.nativeElement.clientHeight;
    if (!width || !height) {
      setTimeout(() => this.renderMap(), 50);
      return;
    }
    const config = {
      container: '#celestial-map',
      width,
      height,
      projection: 'airy',
      datapath: 'https://ofrohn.github.io/data/',
      stars: {
        show: true,
        limit: 6,
        size: 6,
        names: true
      },
      constellations: {
        show: true,
        names: true,
        lines: true
      },
      mw: {
        show: false
      },
      grid: {
        eq: false,
        gal: false
      },
      horizon: {
        show: false
      }
    };

    celestial.display(config);
    this.reparentCelestialNodes();
    this.fitCanvasToHost();
    this.initialized = true;
    this.updateView();
  }

  private updateView(): void {
    const location = [this.lat, this.lon];
    const date = this.date ?? new Date();
    const highlight = this.highlight?.trim().toLowerCase();

    const celestial = (window as any).Celestial;
    if (!celestial) return;
    celestial.skyview({ location, date });
    celestial.redraw();
    this.fitCanvasToHost();
  }

  private reparentCelestialNodes(): void {
    const host = this.celestialHost.nativeElement;
    const body = document.body;
    const nodes = Array.from(body.children).filter((node) => {
      const element = node as HTMLElement;
      return element.tagName === 'CANVAS'
        || element.tagName === 'CONTAINER'
        || element.id === 'celestial-zoomin'
        || element.id === 'celestial-zoomout'
        || element.id === 'error';
    });

    for (const node of nodes) {
      host.appendChild(node);
    }
  }

  private fitCanvasToHost(): void {
    const host = this.celestialHost.nativeElement;
    const canvas = host.querySelector('canvas') as HTMLCanvasElement | null;
    const container = host.querySelector('container') as HTMLElement | null;
    if (!canvas) return;
    const width = host.clientWidth;
    const height = host.clientHeight;
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.style.left = '0';
    canvas.style.top = '0';
    if (container) {
      container.style.width = '100%';
      container.style.height = '100%';
      container.style.left = '0';
      container.style.top = '0';
    }
    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width;
      canvas.height = height;
      const celestial = (window as any).Celestial;
      if (celestial?.resize) {
        celestial.resize();
      } else if (celestial?.redraw) {
        celestial.redraw();
      }
    }
  }
}
