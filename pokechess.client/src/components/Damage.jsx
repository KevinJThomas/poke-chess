import { cn } from "../util";

export default function Damage({ damage, damageType = "normal" }) {
  return (
    <div
      className={cn(
        damageType === "critical" && "scale-125 bg-red-400",
        damageType === "normal" && "bg-amber-400",
        damageType === "weak" && "bg-gray-400",
        "animate-jump-in absolute bottom-[24px] left-[24px] z-5 flex h-8 w-8 items-center justify-center rounded-full border-2",
      )}
    >
      <span className="text-lg font-bold">-{damage}</span>
    </div>
  );
}
