import { cn } from "../util";
import { FireIcon } from "@heroicons/react/24/solid";
import { BoltIcon } from "@heroicons/react/24/solid";

export default function Attack({ attack, isBuffed, burning, shock }) {
  return (
    <div
      style={{ minWidth: 24 }}
      className={cn(
        "absolute bottom-0 left-0 z-5 flex h-6 items-center justify-center rounded-full bg-amber-400",
        isBuffed && "font-semibold",
      )}
    >
      {burning && <FireIcon className="size-3 text-amber-700" />}
      {shock && <BoltIcon className="size-3 text-amber-700" />}
      <span className="text-sm">{attack}</span>
    </div>
  );
}
