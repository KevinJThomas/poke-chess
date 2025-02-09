import { cn, toRoman } from "../util";

export default function Tier({ tier, className }) {
  return (
    <div
      className={cn(
        "absolute top-0 left-0 z-5 flex h-6 w-6 items-center justify-center rounded-full bg-purple-400 text-black",
        className,
      )}
    >
      <span className="text-sm">{toRoman(tier)}</span>
    </div>
  );
}
