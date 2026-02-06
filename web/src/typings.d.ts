declare module 'd3-celestial';

interface Window {
  Celestial?: {
    display: (config: Record<string, unknown>) => void;
    skyview: (options: { location: [number, number]; date: Date }) => void;
    redraw: () => void;
  };
}
