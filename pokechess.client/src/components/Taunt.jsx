import { ShieldCheckIcon } from "@heroicons/react/24/solid";

export default function Taunt() {
  return (
    <div className="absolute bottom-0 left-[28px] z-5 flex h-6 w-6 items-center justify-center rounded-full bg-slate-400">
      <ShieldCheckIcon className="size-4 text-slate-700" />
      <div className="absolute h-2 w-2 bg-slate-700"></div>
    </div>
  );
}
