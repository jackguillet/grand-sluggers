# Authored swing validation (#503 / #541)

`739cf24-contact-only-insufficient.json` is a negative visual fixture: its existing contact/grip assertions pass, but rendered load/follow-through directions are wrong. The Rio MAX load image shows a low, horizontal barrel. Do not treat the JSON `ok` as appearance acceptance.

Measured Rio MAX load tip minus grip: (-2.630, -1.054, +0.996) feet; intended authored load direction rises. Follow-through is nearly opposite the intended direction. A constant axis fitted only at contact does not validate the complete take.

The capture now reapplies the requested pose on the exact capture frame; normal ActorDirector updates previously overwrote the test pose during intervening frames. Final evidence must cover batting-ready, normal/MAX load, contact, and follow-through, both batting hands, all shared body proportions, and the Generic Fenn package. Human appearance acceptance remains open.
