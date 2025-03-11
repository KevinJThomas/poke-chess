import { Dialog, DialogPanel } from "@headlessui/react";
import { useState } from "react";
import Pokemon from "./Pokemon";
import Button from "./Button";
import { cn } from "../util";

export default function DiscoverModal({ discoverOptions, connection }) {
  let [isVisible, setIsVisible] = useState(true);
  
  function selectPokemon(id) {
    connection.invoke("MoveCard", id, 5, null, null);
  }

  function toggleModalVisibility() {
    setIsVisible((prev) => !prev);
  }

  return (
    <Dialog
      open={discoverOptions.length > 0}
      onClose={() => {}}
      className="relative z-8"
    >
      <div
        className={cn(
          "fixed inset-0 flex w-screen items-center justify-center",
        )}
      >
        <DialogPanel
          className={cn(
            isVisible &&
              "border border-neutral-600 bg-amber-200 p-3 [box-shadow:0px_4px_1px_#a3a3a3]",
            !isVisible && "bg-opacity-0",
            "relative flex max-w-lg flex-row rounded-md",
          )}
        >
          {discoverOptions.map((option) => (
            <button
              key={option.id}
              className={cn(
                "cursor-pointer px-2",
                isVisible && "inline",
                !isVisible && "hidden",
              )}
              onClick={() => selectPokemon(option.id)}
            >
              <Pokemon {...option} showTier={true} />
            </button>
          ))}
          {discoverOptions.length > 0 && (
            <Button
              className="fixed top-2 right-2"
              onClick={toggleModalVisibility}
            >
              {isVisible ? "Hide" : "Show"}
            </Button>
          )}
        </DialogPanel>
      </div>
    </Dialog>
  );
}
