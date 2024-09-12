/* -------------------------------------------------------------------------- */
/*                           Stopwatch utility class                          */
/* -------------------------------------------------------------------------- */

export default class Stopwatch {
  public formattedSeconds: number = 0;

  private startTime: number = 0;
  private elapsedTime: number = 0;
  private isRunning: boolean = false;
  private timerInterval: NodeJS.Timeout | null = null;

  constructor() {
    this.reset();
  }

  public update() {
    if (this.isRunning) {
      this.elapsedTime = Date.now() - this.startTime;
      this.formattedSeconds = this.elapsedTime / 1000;
    }
  }

  public start() {
    if (!this.isRunning) {
      this.startTime = Date.now() - this.elapsedTime;
      this.timerInterval = setInterval(() => this.update(), 100); // Update every 100ms
      this.isRunning = true;
    }
  }

  public stop() {
    if (this.isRunning) {
      clearInterval(this.timerInterval!);
      this.isRunning = false;
    }
  }

  public reset() {
    if (this.isRunning) {
      this.stop();
    }
    this.elapsedTime = 0;
    this.startTime = 0;
  }

  public getElapsedTime(): number {
    this.update();
    return this.elapsedTime;
  }

  public getFormattedTime(): string {
    const seconds = Math.floor(this.getElapsedTime() / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const secondsDisplay = (seconds % 60).toString().padStart(2, "0");
    const minutesDisplay = (minutes % 60).toString().padStart(2, "0");
    const hoursDisplay = hours.toString().padStart(2, "0");

    return `${hoursDisplay}:${minutesDisplay}:${secondsDisplay}`;
  }
}
