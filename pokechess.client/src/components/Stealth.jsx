import { EyeSlashIcon } from "@heroicons/react/24/solid";

export default function Sealth() {
  return (
    <div className="absolute top-0 right-0 z-5 flex h-6 w-6 items-center justify-center rounded-full bg-fuchsia-900">
      <EyeSlashIcon className="size-4 text-slate-200" />
    </div>
  );
}
