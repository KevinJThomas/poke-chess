import { BoltIcon } from "@heroicons/react/24/solid";

export default function Paralyze() {
  return (
    <div className="absolute -top-4 left-[28px] z-5 flex h-6 w-6 items-center justify-center rounded-full bg-yellow-700">
      <BoltIcon className="size-4 text-slate-200" />
    </div>
  );
}
