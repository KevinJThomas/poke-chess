import { cn } from "../util";

export default function Health({ health, animate, isBuffed }) {
  return (
    <div
      style={{ minWidth: 24 }}
      className={cn(
        "absolute right-0 bottom-0 z-5 flex h-6 items-center justify-center rounded-full bg-red-400",
        isBuffed && "font-semibold",
      )}
    >
      {animate && (
        <span className="absolute inline-flex h-3/4 w-3/4 animate-ping rounded-full bg-red-400 opacity-75"></span>
      )}
      <span className="z-5 text-sm">{health}</span>
    </div>
  );
}
